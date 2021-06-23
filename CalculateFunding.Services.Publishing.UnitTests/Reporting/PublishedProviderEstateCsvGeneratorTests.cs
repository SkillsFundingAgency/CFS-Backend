using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language;
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
        private Mock<IJobManagement> _jobManagement;

        private BlobProperties _blobProperties;
        
        private string _rootPath;

        private Message _message;

        private const string PublishedFundingReportContainerName = "publishingreports";

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
            _jobManagement = new Mock<IJobManagement>();
            
            _service = new PublishedProviderEstateCsvGenerator(
                _jobManagement.Object,
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
            string expectedInterimFilePath = Path.Combine(_rootPath, $"funding-lines-{specificationId}-HistoryPublishedProviderEstate-{fundingPeriodId}.csv");

            GivenTheMessageProperties(("specification-id", specificationId), 
                ("jobId", jobId),
                ("funding-period-id", fundingPeriodId),
                ("funding-stream-id", fundingStreamId));
            AndTheFileExists(expectedInterimFilePath);
            AndTheJobExists(jobId);
            AndTheRefreshedProviderVersionBatchProcessingFeed(specificationId, new Mock<ICosmosDbFeedIterator>().Object);

            await WhenTheCsvIsGenerated();

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, null, null),
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
                .Verify(_ => _.UploadFileAsync(_cloudBlob.Object, It.IsAny<Stream>()),
                    Times.Never);

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, true, null),
                Times.Once);
        }

        [TestMethod]
        public void TransformsPublishedProvidersForSpecificationInBatchesAndFailsJobIfUploadFails()
        {
            string specificationId = NewRandomString();
            string jobId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fileName = $"funding-lines-{specificationId}-HistoryPublishedProviderEstate-{fundingPeriodId}.csv";
            string expectedInterimFilePath = Path.Combine(_rootPath, fileName);
            string jobDefinitionName = JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob;

            IEnumerable<PublishedProviderVersion> publishedProviderVersionsOne = new[]
            {
                new PublishedProviderVersion(),
            };

            ExpandoObject[] transformedRowsOne = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
            };

            string expectedCsvOne = NewRandomString();

            MemoryStream incrementalFileStream = new MemoryStream();

            Mock<ICosmosDbFeedIterator> feed = new Mock<ICosmosDbFeedIterator>();

            GivenTheCsvRowTransformation(transformedRowsOne, expectedCsvOne, true);
            AndTheMessageProperties(("specification-id", specificationId),
                ("jobId", jobId),
                ("funding-period-id", fundingPeriodId),
                ("funding-stream-id", fundingStreamId));
            AndTheCloudBlobForSpecificationId(fileName, PublishedFundingReportContainerName);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            AndTheTransformForJobDefinition(jobDefinitionName);
            AndTheJobCanBeRun(jobId);
            AndTheJobExists(jobId);
            string errorMessage = "Unable to complete csv generation job.";
            AndTheBlobThrowsError(incrementalFileStream, new NonRetriableException(errorMessage));
            AndTheRefreshedProviderVersionBatchProcessingFeed(specificationId, feed.Object);
            AndTheFeedIteratorHasThePages(feed, publishedProviderVersionsOne);

            Func<Task> test = async () => await WhenTheCsvIsGenerated();

            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, null, null),
                Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath,
                        It.IsAny<string>(),
                        default),
                    Times.Exactly(1));

            _blobClient
                .Verify(_ => _.UploadFileAsync(_cloudBlob.Object, incrementalFileStream),
                    Times.Once);

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, false, errorMessage),
                Times.Once);

            _blobProperties.ContentDisposition
                .Should()
                .StartWith($"attachment; filename={fundingStreamId} {fundingPeriodId} Provider Estate Variations {DateTimeOffset.UtcNow:yyyy-MM-dd}");
        }

        [TestMethod]
        public async Task TransformsPublishedProvidersForSpecificationInBatchesAndCreatesCsvWithResults()
        {
            string specificationId = NewRandomString();
            string jobId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fileName = $"funding-lines-{specificationId}-HistoryPublishedProviderEstate-{fundingPeriodId}.csv";
            string expectedInterimFilePath = Path.Combine(_rootPath, fileName);
            string jobDefinitionName = JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob;

            IEnumerable<PublishedProviderVersion> publishedProviderVersionsOne = new []
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
            
            string expectedCsvOne = NewRandomString();
            
            MemoryStream incrementalFileStream = new MemoryStream();

            Mock<ICosmosDbFeedIterator> feed = new Mock<ICosmosDbFeedIterator>();

            GivenTheCsvRowTransformation(transformedRowsOne, expectedCsvOne, true);
            AndTheMessageProperties(("specification-id", specificationId), 
                ("jobId", jobId),
                ("funding-period-id", fundingPeriodId),
                ("funding-stream-id", fundingStreamId));
            AndTheCloudBlobForSpecificationId(fileName, PublishedFundingReportContainerName);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            AndTheTransformForJobDefinition(jobDefinitionName);
            AndTheJobExists(jobId);
            AndTheRefreshedProviderVersionBatchProcessingFeed(specificationId, feed.Object);
            AndTheFeedIteratorHasThePages(feed, publishedProviderVersionsOne, publishedProviderVersionsTwo);

            await WhenTheCsvIsGenerated();
            
            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, null, null),
                Times.Once);
            
            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath, 
                        It.IsAny<string>(), 
                        default),
                    Times.Exactly(2));
            
            _blobClient
                .Verify(_ => _.UploadFileAsync(_cloudBlob.Object, incrementalFileStream),
                    Times.Once);

            AndBlobMetadataSet(specificationId);

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, null, null),
                Times.Once);

            _blobProperties.ContentDisposition
                .Should()
                .StartWith($"attachment; filename={fundingStreamId} {fundingPeriodId} Provider Estate Variations {DateTimeOffset.UtcNow:yyyy-MM-dd}");
        }
        
        [TestMethod]
        public async Task TransformsPublishedProviderVersionsForSpecificationInBatchesAndCreatesCsvWithResults()
        {
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string jobId = NewRandomString();
            string fileName = $"funding-lines-{specificationId}-HistoryPublishedProviderEstate-{fundingPeriodId}.csv";
            string expectedInterimFilePath = Path.Combine(_rootPath, fileName);
            string jobDefinitionName = JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob;

            IEnumerable<PublishedProviderVersion> publishedProviderVersionsOne = new []
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
            
            Mock<ICosmosDbFeedIterator> feed = new Mock<ICosmosDbFeedIterator>();

            GivenTheCsvRowTransformation(transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(transformedRowsTwo, expectedCsvTwo, false);
            AndTheMessageProperties(("specification-id", specificationId), ("jobId", jobId), ("funding-period-id", fundingPeriodId));
            AndTheCloudBlobForSpecificationId(fileName, PublishedFundingReportContainerName);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            AndTheTransformForJobDefinition(jobDefinitionName);
            AndTheJobCanBeRun(jobId);
            AndTheJobExists(jobId);
            AndTheRefreshedProviderVersionBatchProcessingFeed(specificationId, feed.Object);
            AndTheFeedIteratorHasThePages(feed, publishedProviderVersionsOne, publishedProviderVersionsTwo);

            await WhenTheCsvIsGenerated();

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, null, null),
                Times.Once);
            
            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);
            
            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath, 
                        It.IsAny<string>(), 
                        default),
                    Times.Exactly(2));
            
            _blobClient
                .Verify(_ => _.UploadFileAsync(_cloudBlob.Object, incrementalFileStream),
                    Times.Once);

            AndBlobMetadataSet(specificationId);

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, true, null));
        }

        private void AndBlobMetadataSet(string specificationId)
        {
            _blobClient
            .Verify(_ => _.AddMetadataAsync(
                _cloudBlob.Object,
                It.Is<IDictionary<string, string>>(d => 
                    d.ContainsKey("specification-id") && d["specification-id"] == specificationId)),
                Times.Once);
        }

        private void AndTheJobCanBeRun(string jobId)
        {
            _jobManagement.Setup(_ => _.RetrieveJobAndCheckCanBeProcessed(jobId))
                .ReturnsAsync(new Common.ApiClient.Jobs.Models.JobViewModel { Id = jobId });
        }

        private void AndTheJobExists(string jobId)
        {
            _jobManagement.Setup(_ => _.UpdateJobStatus(jobId, 0, 0, null, null));
        }

        private void AndTheBlobThrowsError(Stream incrementalFileStream, Exception exception)
        {
            _blobClient.Setup(_ => _.UploadFileAsync(_cloudBlob.Object, incrementalFileStream))
                .Throws(exception);
        }

        private void AndTheTransformForJobDefinition(string jobDefinitionName)
        {
            _publishedProviderCsvTransformServiceLocator.Setup(_ => _.GetService(jobDefinitionName))
                .Returns(_transformation.Object);
        }

        private void AndTheCloudBlobForSpecificationId(string fileName, string containerName)
        {
            _blobClient
                .Setup(_ => _.GetBlockBlobReference(fileName, containerName))
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

        private void AndTheCsvRowTransformation(IEnumerable<ExpandoObject> transformedRows,
            string csv,
            bool outputHeaders)
            => GivenTheCsvRowTransformation(transformedRows, csv, outputHeaders);

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
        
        private void GivenTheRefreshedProviderVersionBatchProcessingFeed(string specificationId,
            ICosmosDbFeedIterator feed)
            => _publishedFunding.Setup(_ => _.GetRefreshedProviderVersionBatchProcessing(specificationId,
                    PublishedProviderEstateCsvGenerator.BatchSize))
                .Returns(feed);

        private void AndTheRefreshedProviderVersionBatchProcessingFeed(string specification,
            ICosmosDbFeedIterator feed)
            => GivenTheRefreshedProviderVersionBatchProcessingFeed(specification, feed);
        
        private void AndTheFeedIteratorHasThePages<TEntity>(Mock<ICosmosDbFeedIterator> feed,
            params IEnumerable<TEntity>[] pages) where TEntity : IIdentifiable
        {
            ISetupSequentialResult<bool> hasMoreRecordsSequence = feed.SetupSequence(_ => _.HasMoreResults);
            ISetupSequentialResult<Task<IEnumerable<TEntity>>> readNextSequence 
                = feed.SetupSequence(_ => _.ReadNext<TEntity>(It.IsAny<CancellationToken>()));

            foreach (IEnumerable<TEntity> page in pages)
            {
                hasMoreRecordsSequence = hasMoreRecordsSequence.Returns(true);
                readNextSequence = readNextSequence.ReturnsAsync(page);
            }
        }
    }
}