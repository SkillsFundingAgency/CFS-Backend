using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting;
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
    public class FundingLineCsvGeneratorTests
    {
        private FundingLineCsvGenerator _service;

        private Mock<IFundingLineCsvTransformServiceLocator> _transformServiceLocator;
        private Mock<IPublishedFundingPredicateBuilder> _predicateBuilder;
        private Mock<ICsvUtils> _csvUtils;
        private Mock<IBlobClient> _blobClient;
        private Mock<ICloudBlob> _cloudBlob; 
        private Mock<IFundingLineCsvTransform> _transformation;
        private Mock<IPublishedFundingRepository> _publishedFunding;
        private Mock<IFileSystemAccess> _fileSystemAccess;
        private Mock<IFileSystemCacheSettings> _fileSystemCacheSettings;
        private string _rootPath;

        private Message _message;
        
        [TestInitialize]
        public void SetUp()
        {
            _predicateBuilder = new Mock<IPublishedFundingPredicateBuilder>();
            _transformServiceLocator = new Mock<IFundingLineCsvTransformServiceLocator>();
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _blobClient = new Mock<IBlobClient>();
            _csvUtils = new Mock<ICsvUtils>();
            _transformation = new Mock<IFundingLineCsvTransform>();
            _cloudBlob = new Mock<ICloudBlob>();
            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemCacheSettings = new Mock<IFileSystemCacheSettings>();
            
            _service = new FundingLineCsvGenerator(_transformServiceLocator.Object,
                _predicateBuilder.Object,
                _blobClient.Object,
                _publishedFunding.Object,
                _csvUtils.Object,
                _fileSystemAccess.Object,
                _fileSystemCacheSettings.Object,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                new Mock<ILogger>().Object);
            
            _message = new Message();
            _rootPath = NewRandomString();

            _fileSystemCacheSettings.Setup(_ => _.Path)
                .Returns(_rootPath);

            _fileSystemAccess.Setup(_ => _.Append(It.IsAny<string>(), 
                    It.IsAny<string>(), default))
                .Returns(Task.CompletedTask);
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
        public void ThrowsExceptionIfNoJobTypeInMessageProperties()
        {
            GivenTheMessageProperties(("specification-id", NewRandomString()));
            
            Func<Task> invocation = WhenTheCsvIsGenerated;

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .WithMessage("Specification id missing");
        }

        [TestMethod]
        [DynamicData(nameof(JobTypeExamples), DynamicDataSourceType.Method)]
        public async Task TransformsProviderResultsForSpecificationInBatchesAndCreatesCsvWithResults(
            FundingLineCsvGeneratorJobType jobType)
        {
            string specificationId = NewRandomString();
            string expectedInterimFilePath = Path.Combine(_rootPath, $"funding-lines-{jobType}-{specificationId}.csv");
            
            IEnumerable<PublishedProvider> publishProvidersOne = new []
            {
                new PublishedProvider(),
            };
            IEnumerable<PublishedProvider> publishedProvidersTwo = new []
            {
                new PublishedProvider(),
                new PublishedProvider(),
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
            
            StreamWriter streamWriterOne = new StreamWriter(new MemoryStream(expectedCsvOne.AsUTF8Bytes()));
            StreamWriter streamWriterTwo = new StreamWriter(new MemoryStream(expectedCsvTwo.AsUTF8Bytes()));
            
            MemoryStream incrementalFileStream = new MemoryStream();

            string predicate = NewRandomString();

            GivenTheCsvRowTransformation(publishProvidersOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(publishedProvidersTwo, transformedRowsTwo, expectedCsvTwo,  false);
            AndTheMessageProperties(("specification-id", specificationId), ("job-type", jobType.ToString()));
            AndTheCloudBlobForSpecificationId(specificationId, jobType);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            AndTheTransformForJobType(jobType);
            AndThePredicate(jobType, predicate);

            _publishedFunding.Setup(_ => _.PublishedProviderBatchProcessing(predicate,
                    specificationId,
                    It.IsAny<Func<List<PublishedProvider>, Task>>(),
                    100))
                .Callback<string, string, Func<List<PublishedProvider>, Task>, int>((pred, spec,  batchProcessor, batchSize) =>
                {
                    batchProcessor(publishProvidersOne.ToList())
                        .GetAwaiter()
                        .GetResult();
                    
                    batchProcessor(publishedProvidersTwo.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);

            await WhenTheCsvIsGenerated();
            
            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);

            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath, 
                        expectedCsvOne, 
                        default),
                    Times.Once);
            
            _fileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath, 
                        expectedCsvTwo, 
                        default),
                    Times.Once);
            
            _blobClient
                .Verify(_ => _.UploadAsync(_cloudBlob.Object, incrementalFileStream),
                    Times.Once);
        }

        private void AndThePredicate(FundingLineCsvGeneratorJobType jobType, string predicate)
        {
            _predicateBuilder.Setup(_ => _.BuildPredicate(jobType))
                .Returns(predicate);
        }

        private void AndTheTransformForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            _transformServiceLocator.Setup(_ => _.GetService(jobType))
                .Returns(_transformation.Object);
        }

        private static IEnumerable<object[]> JobTypeExamples()
        {
            yield return new object [] {FundingLineCsvGeneratorJobType.CurrentState};
        }

        private void AndTheCloudBlobForSpecificationId(string specificationId, FundingLineCsvGeneratorJobType jobType)
        {
            _blobClient
                .Setup(_ => _.GetBlockBlobReference($"funding-lines-{jobType}-{specificationId}.csv"))
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

        private void AndTheCsvRowTransformation(IEnumerable<PublishedProvider> publishedProviders, ExpandoObject[] transformedRows, string csv, bool outputHeaders)
        {
            GivenTheCsvRowTransformation(publishedProviders, transformedRows, csv, outputHeaders);
        }

        private void GivenTheCsvRowTransformation(IEnumerable<PublishedProvider> publishedProviders, IEnumerable<ExpandoObject> transformedRows, string csv, bool outputHeaders)
        {
            _transformation
                .Setup(_ => _.Transform(publishedProviders))
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