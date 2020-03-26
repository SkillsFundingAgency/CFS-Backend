using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class PublishedProviderEstateCsvGeneratorTests
    {
        private PublishedProviderEstateCsvGenerator _service;

        private Mock<IPublishedProviderCsvTransformServiceLocator> _publishedProviderCsvTransformServiceLocator;
        private Mock<ICsvUtils> _csvUtils;
        private Mock<IBlobClient> _blobClient;
        private Mock<ICloudBlob> _cloudBlob; 
        private Mock<IPublishedProviderCsvTransform> _transformation;
        private Mock<IPublishedFundingRepository> _publishedFunding;
        private Mock<IFileSystemAccess> _fileSystemAccess;
        private Mock<IFileSystemCacheSettings> _fileSystemCacheSettings;
        private Mock<IJobTracker> _jobTracker;

        private BlobProperties _blobProperties;
        
        private string _rootPath;

        private Message _message;
        
        [TestInitialize]
        public void SetUp()
        {
            _publishedProviderCsvTransformServiceLocator = new Mock<IPublishedProviderCsvTransformServiceLocator>();
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _blobClient = new Mock<IBlobClient>();
            _csvUtils = new Mock<ICsvUtils>();
            _transformation = new Mock<IPublishedProviderCsvTransform>();
            _cloudBlob = new Mock<ICloudBlob>();
            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemCacheSettings = new Mock<IFileSystemCacheSettings>();
            _jobTracker = new Mock<IJobTracker>();
            
            _service = new PublishedProviderEstateCsvGenerator(
                _jobTracker.Object,
                _fileSystemAccess.Object,
                _fileSystemCacheSettings.Object,
                _blobClient.Object,
                _publishedFunding.Object,
                _csvUtils.Object,
                new Mock<ILogger>().Object,
                _publishedProviderCsvTransformServiceLocator.Object,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                });
            
            _message = new Message();
            _rootPath = NewRandomString();

            _fileSystemCacheSettings.Setup(_ => _.Path)
                .Returns(_rootPath);

            _fileSystemAccess.Setup(_ => _.Append(It.IsAny<string>(), 
                    It.IsAny<string>(), default))
                .Returns(Task.CompletedTask);
            
            _blobProperties = new BlobProperties();
            _cloudBlob.Setup(_ => _.Properties)
                .Returns(_blobProperties);
        }

        [TestMethod]
        public void ThrowsExceptionIfNoSpecificationIdInMessageProperties()
        {
            Func<Task> invocation = WhenTheCsvIsGenerated;

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .WithMessage("Specification id missing");
        }
        
        [TestMethod]
        public void ThrowsExceptionIfNoJobIdInMessageProperties()
        {
            GivenTheMessageProperties(("specification-id", NewRandomString()));
            
            Func<Task> invocation = WhenTheCsvIsGenerated;

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .WithMessage("Job id missing");
        }

        [TestMethod]
        public async Task ExitsEarlyIfNoProvidersMatchForTheSpecificationId()
        {
            string specificationId = NewRandomString();
            string jobId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string expectedInterimFilePath = Path.Combine(_rootPath, $"published-provider-estate-{specificationId}.csv");

            GivenTheMessageProperties(("specification-id", specificationId), 
                ("jobId", jobId),
                ("funding-period-id", fundingPeriodId),
                ("funding-stream-id", fundingStreamId));
            AndTheFileExists(expectedInterimFilePath);
            AndTheJobExists(jobId);

            await WhenTheCsvIsGenerated();
            
            _jobTracker.Verify(_ => _.TryStartTrackingJob(jobId, JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob),
                Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath,
                        It.IsAny<string>(),
                        default),
                    Times.Never);

            _blobClient
                .Verify(_ => _.UploadAsync(_cloudBlob.Object, It.IsAny<Stream>()),
                    Times.Never);

            _jobTracker.Verify(_ => _.CompleteTrackingJob(jobId),
                Times.Once);
        }

        [TestMethod]
        public async Task TransformsPublishedProvidersForSpecificationInBatchesAndCreatesCsvWithResults()
        {
            string specificationId = NewRandomString();
            string jobId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string expectedInterimFilePath = Path.Combine(_rootPath, $"published-provider-estate-{specificationId}.csv");
            string jobDefinitionName = JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob;

            IEnumerable<PublishedProviderVersion> publishProviderVersionsOne = new []
            {
                new PublishedProviderVersion(),
            };
            IEnumerable<PublishedProviderVersion> publishedProviderVersionsTwo = new []
            {
                new PublishedProviderVersion(),
                new PublishedProviderVersion()
            };
            
            ExpandoObject[] transformedRowsOne = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
            };
            ExpandoObject[] transformedRowsTwo = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
            };
            
            string expectedCsvOne = NewRandomString();
            string expectedCsvTwo = NewRandomString();
            
            MemoryStream incrementalFileStream = new MemoryStream();

            GivenTheCsvRowTransformation(transformedRowsOne, expectedCsvOne, true);
            AndTheMessageProperties(("specification-id", specificationId), 
                ("jobId", jobId),
                ("funding-period-id", fundingPeriodId),
                ("funding-stream-id", fundingStreamId));
            AndTheCloudBlobForSpecificationId(specificationId);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            AndTheTransformForJobDefinition(jobDefinitionName);
            AndTheJobExists(jobId);
            
            _publishedFunding.Setup(_ => _.RefreshedProviderVersionBatchProcessing(
                    specificationId,
                    It.IsAny<Func<List<PublishedProviderVersion>, Task>>(),
                    It.IsAny<int>()))
                .Callback<string, Func<List<PublishedProviderVersion>, Task>, int>((spec,  batchProcessor, batchSize) =>
                {
                    batchProcessor(publishProviderVersionsOne.ToList())
                        .GetAwaiter()
                        .GetResult();
                    
                    batchProcessor(publishedProviderVersionsTwo.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);

            await WhenTheCsvIsGenerated();
            
            _jobTracker.Verify(_ => _.TryStartTrackingJob(jobId, JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob),
                Times.Once);
            
            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath, 
                        It.IsAny<string>(), 
                        default),
                    Times.Exactly(3));
            
            _blobClient
                .Verify(_ => _.UploadAsync(_cloudBlob.Object, incrementalFileStream),
                    Times.Once);
            
            _jobTracker.Verify(_ => _.CompleteTrackingJob(jobId),
                Times.Once);

            _blobProperties.ContentDisposition
                .Should()
                .StartWith($"attachment; filename=published-provider-estate-{fundingStreamId}-{fundingPeriodId}-{DateTimeOffset.UtcNow:yyyy-MM-dd}");
        }
        
        [TestMethod]
        public async Task TransformsPublishedProviderVersionsForSpecificationInBatchesAndCreatesCsvWithResults()
        {
            string specificationId = NewRandomString();
            string jobId = NewRandomString();
            string expectedInterimFilePath = Path.Combine(_rootPath, $"published-provider-estate-{specificationId}.csv");
            string jobDefinitionName = JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob;

            IEnumerable<PublishedProviderVersion> publishProviderVersionsOne = new []
            {
                new PublishedProviderVersion(),
            };
            IEnumerable<PublishedProviderVersion> publishedProviderVersionsTwo = new []
            {
                new PublishedProviderVersion(),
                new PublishedProviderVersion(),
            };
            
            ExpandoObject[] transformedRowsOne = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
            };
            ExpandoObject[] transformedRowsTwo = {
                new ExpandoObject(),
                new ExpandoObject(),
            };
            
            string expectedCsvOne = NewRandomString();
            string expectedCsvTwo = NewRandomString();
            
            MemoryStream incrementalFileStream = new MemoryStream();

            GivenTheCsvRowTransformation(transformedRowsOne, expectedCsvOne, true);
            AndTheMessageProperties(("specification-id", specificationId), ("jobId", jobId));
            AndTheCloudBlobForSpecificationId(specificationId);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            AndTheTransformForJobDefinition(jobDefinitionName);
            AndTheJobExists(jobId);

            _publishedFunding.Setup(_ => _.RefreshedProviderVersionBatchProcessing(
                    specificationId,
                    It.IsAny<Func<List<PublishedProviderVersion>, Task>>(),
                    It.IsAny<int>()))
                .Callback<string, Func<List<PublishedProviderVersion>, Task>, int>((spec,  batchProcessor, batchSize) =>
                {
                    batchProcessor(publishProviderVersionsOne.ToList())
                        .GetAwaiter()
                        .GetResult();
                    
                    batchProcessor(publishedProviderVersionsTwo.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);

            await WhenTheCsvIsGenerated();
            
            _jobTracker.Verify(_ => _.TryStartTrackingJob(jobId, JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob),
                Times.Once);
            
            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);
            
            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath, 
                        It.IsAny<string>(), 
                        default),
                    Times.Exactly(3));
            
            _blobClient
                .Verify(_ => _.UploadAsync(_cloudBlob.Object, incrementalFileStream),
                    Times.Once);
            
            _jobTracker.Verify(_ => _.CompleteTrackingJob(jobId));
        }

        private void AndTheJobExists(string jobId)
        {
            _jobTracker.Setup(_ => _.TryStartTrackingJob(jobId, JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob))
                .ReturnsAsync(true);
        }

        private void AndTheTransformForJobDefinition(string jobDefinitionName)
        {
            _publishedProviderCsvTransformServiceLocator.Setup(_ => _.GetService(jobDefinitionName))
                .Returns(_transformation.Object);
        }

        private void AndTheCloudBlobForSpecificationId(string specificationId)
        {
            _blobClient
                .Setup(_ => _.GetBlockBlobReference($"published-provider-estate-{specificationId}.csv"))
                .Returns(_cloudBlob.Object);
        }

        private void AndTheFileStream(string path, Stream stream)
        {
            _fileSystemAccess.Setup(_ => _.OpenRead(path))
                .Returns(stream);
        }

        private void AndTheFileExists(string path)
        {
            _fileSystemAccess.Setup(_ => _.Exists(path))
                .Returns(true);
        }

        private void GivenTheCsvRowTransformation(IEnumerable<ExpandoObject> transformedRows, string csv, bool outputHeaders)
        {
            _transformation
                .Setup(_ => _.Transform(It.IsAny<IEnumerable<dynamic>>()))
                .Returns(transformedRows);

            _csvUtils
                .Setup(_ => _.AsCsv(transformedRows, outputHeaders))
                .Returns(csv);
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private async Task WhenTheCsvIsGenerated()
        {
            await _service.Run(_message);
        }

        private void AndTheMessageProperties(params (string, string)[] properties)
        {
            GivenTheMessageProperties(properties);            
        }

        private void GivenTheMessageProperties(params (string,string)[] properties)
        {
            _message.AddUserProperties(properties);
        }   
    }
}