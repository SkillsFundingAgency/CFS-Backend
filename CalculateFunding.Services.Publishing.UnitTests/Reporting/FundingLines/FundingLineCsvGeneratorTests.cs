using System;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class FundingLineCsvGeneratorTests
    {
        private FundingLineCsvGenerator _service;

        private Mock<IFundingLineCsvTransformServiceLocator> _transformServiceLocator;
        private Mock<IFundingLineCsvBatchProcessorServiceLocator> _batchProcessorServiceLocator;
        private Mock<IPublishedFundingPredicateBuilder> _predicateBuilder;
        private Mock<IBlobClient> _blobClient;
        private Mock<ICloudBlob> _cloudBlob; 
        private Mock<IFundingLineCsvTransform> _transformation;
        private Mock<IFileSystemAccess> _fileSystemAccess;
        private Mock<IFileSystemCacheSettings> _fileSystemCacheSettings;
        private Mock<IJobManagement> _jobManagement;
        private Mock<IFundingLineCsvBatchProcessor> _batchProcessor;
        private BlobProperties _blobProperties;
        private string _rootPath;

        private Message _message;

        private const string PublishedFundingReportContainerName = "publishingreports";

        [TestInitialize]
        public void SetUp()
        {
            _predicateBuilder = new Mock<IPublishedFundingPredicateBuilder>();
            _transformServiceLocator = new Mock<IFundingLineCsvTransformServiceLocator>();
            _batchProcessorServiceLocator = new Mock<IFundingLineCsvBatchProcessorServiceLocator>();
            _batchProcessor = new Mock<IFundingLineCsvBatchProcessor>();
            _blobClient = new Mock<IBlobClient>();
            _transformation = new Mock<IFundingLineCsvTransform>();
            _cloudBlob = new Mock<ICloudBlob>();
            _fileSystemAccess = new Mock<IFileSystemAccess>();
            _fileSystemCacheSettings = new Mock<IFileSystemCacheSettings>();
            _jobManagement = new Mock<IJobManagement>();
            
            _service = new FundingLineCsvGenerator(_transformServiceLocator.Object,
                _predicateBuilder.Object,
                _blobClient.Object,
                _fileSystemAccess.Object,
                _fileSystemCacheSettings.Object,
                _batchProcessorServiceLocator.Object,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync()
                },
                _jobManagement.Object,
                Logger.None);
            
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
        public void ThrowsExceptionIfNoJobIdInMessageProperties()
        {
            GivenTheMessageProperties(("specification-id", NewRandomString()), ("job-type", "History"));
            
            Func<Task> invocation = WhenTheCsvIsGenerated;

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .WithMessage("Job id missing");
        }

        [TestMethod]
        public async Task ExitsEarlyIfNoProvidersMatchForTheJobTypePredicate()
        {
            string specificationId = NewRandomString();
            string fundingLineName = NewRandomString();
            string jobId = NewRandomString();
            string expectedInterimFilePath = Path.Combine(_rootPath, $"funding-lines-{specificationId}-Released-{fundingLineName}.csv");
            FundingLineCsvGeneratorJobType jobType = FundingLineCsvGeneratorJobType.Released;

            GivenTheMessageProperties(("specification-id", specificationId), ("job-type", jobType.ToString()), ("jobId", jobId), ("funding-line-name", fundingLineName));
            AndTheFileExists(expectedInterimFilePath);
            AndTheJobExists(jobId);
            AndTheTransformForJobType(jobType);
            AndTheBatchProcessorForJobType(jobType);

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
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, "spec1", null, null, null, "AY-1920",
            "funding-lines-spec1-CurrentState.csv",
            " AY-1920 Provider Funding Lines Current State", false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, "spec2", "FC1", "FN1", "DSG", "AY-2020",
            "funding-lines-spec2-Released-FN1-DSG.csv",
            "DSG AY-2020 Provider Funding Lines Released Only", false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, "spec2", "FC1", "FN–1", "DSG", "AY-2020",
            "funding-lines-spec2-Released-FN1-DSG.csv",
            "DSG AY-2020 Provider Funding Lines Released Only", false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, "spec3", null, null, "PSG", "AY-2021",
            "funding-lines-spec3-CurrentProfileValues-PSG.csv",
            "PSG AY-2021  Profile Current State", false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, "spec3", null, null, "PSG", "AY-2021",
            "funding-lines-spec3-CurrentProfileValues-PSG.csv",
            "PSG AY-2021  Profile Current State", false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, "spec3", null, null, "PSG", "AY-2021",
            "funding-lines-spec3-CurrentProfileValues-PSG.csv",
            "PSG AY-2021  Profile Current State", true)]
        public async Task TransformsPublishedProvidersForSpecificationInBatchesAndCreatesCsvWithResultsOrFailJob(
            FundingLineCsvGeneratorJobType jobType,
            string specificationId,
            string fundingLineCode,
            string fundingLineName,
            string fundingStreamId,
            string fundingPeriodId,
            string expectedFileName,
            string expectedContentDisposition,
            bool throwException)
        {
            string jobId = NewRandomString();
            string expectedInterimFilePath = Path.Combine(_rootPath, expectedFileName);
            
            MemoryStream incrementalFileStream = new MemoryStream();

            string predicate = NewRandomString();

            GivenTheMessageProperties(("specification-id", specificationId), 
                ("job-type", jobType.ToString()), 
                ("jobId", jobId), 
                ("funding-line-code", fundingLineCode),
                ("funding-line-name", fundingLineName),
                ("funding-period-id", fundingPeriodId), 
                ("funding-stream-id", fundingStreamId));
            AndTheCloudBlobForFileName(expectedFileName);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            AndTheTransformForJobType(jobType);
            AndThePredicate(jobType, predicate);
            AndTheJobExists(jobId);
            AndTheBatchProcessorForJobType(jobType);
            AndTheBatchProcessorProcessedResults(jobType, specificationId, fundingPeriodId, expectedInterimFilePath, fundingLineName, fundingStreamId, fundingLineCode);

            string errorMessage = "Unable to complete funding line csv generation job.";

            if (throwException)
            {
                AndTheBlobThrowsError(incrementalFileStream, new NonRetriableException(errorMessage));

                Func<Task> test = async () => await WhenTheCsvIsGenerated();

                test
                    .Should()
                    .ThrowExactly<NonRetriableException>()
                    .Which
                    .Message
                    .Should()
                    .Be(errorMessage);
            }
            else
            {
                await WhenTheCsvIsGenerated();
            }

            _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, null, null),
                Times.Once);
            
            _fileSystemAccess
                .Verify(_ => _.Delete(expectedInterimFilePath),
                    Times.Once);

            _blobProperties?.ContentDisposition
                .Should()
                .StartWith($"attachment; filename={expectedContentDisposition} {DateTimeOffset.UtcNow:yyyy-MM-dd}")
                .And
                .NotContain(":");
            
            _blobClient
                .Verify(_ => _.UploadFileAsync(_cloudBlob.Object, incrementalFileStream),
                    Times.Once);

            if (throwException)
            {
                _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, false, errorMessage),
                Times.Once);
            }
            else
            {
                _jobManagement.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, true, null),
                Times.Once);
            }
        }

        private void AndThePredicate(FundingLineCsvGeneratorJobType jobType, string predicate)
        {
            _predicateBuilder.Setup(_ => _.BuildPredicate(jobType))
                .Returns(predicate);
        }

        private void AndTheJobExists(string jobId)
        {
            _jobManagement.Setup(_ => _.UpdateJobStatus(jobId, 0, 0, false, null));
        }

        private void AndTheBatchProcessorProcessedResults(FundingLineCsvGeneratorJobType jobType,
            string specificationId,
            string fundingPeriodId,
            string filePath,
            string fundingLineName,
            string fundingStreamId,
            string fundingLineCode)
        {
            _batchProcessor.Setup(_ => _.GenerateCsv(jobType, specificationId, fundingPeriodId, filePath, _transformation.Object, fundingLineName, fundingStreamId, fundingLineCode))
                .ReturnsAsync(true)
                .Verifiable();
        }

        private void AndTheBatchProcessorForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            _batchProcessorServiceLocator.Setup(_ => _.GetService(jobType))
                .Returns(_batchProcessor.Object);
        }

        private void AndTheTransformForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            _transformServiceLocator.Setup(_ => _.GetService(jobType))
                .Returns(_transformation.Object);
        }

        private void AndTheCloudBlobForFileName(string fileName)
        {
            _blobClient
                .Setup(_ => _.GetBlockBlobReference(fileName, PublishedFundingReportContainerName))
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

        private void AndTheBlobThrowsError(Stream incrementalFileStream, Exception exception)
        {
            _blobClient.Setup(_ => _.UploadFileAsync(_cloudBlob.Object, incrementalFileStream))
                .Throws(exception);
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private async Task WhenTheCsvIsGenerated()
        {
            await _service.Run(_message);
        }

        private void GivenTheMessageProperties(params (string,string)[] properties)
        {
            _message.AddUserProperties(properties);
        }   
    }
}