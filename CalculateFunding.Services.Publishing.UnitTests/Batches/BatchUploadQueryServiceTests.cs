using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Batches
{
    [TestClass]
    public class BatchUploadServiceTests
    {
        private const string ContainerName = "batchuploads";
        private Mock<IDateTimeProvider> _dateTimeProvider;
        private Mock<IUniqueIdentifierProvider> _uuidProvider;
        private Mock<IBlobClient> _blobClient;
        private Mock<ICloudBlob> _blob;
        
        private BatchUploadService _service;

        private byte[] _uploadedBytes;
        
        [TestInitialize]
        public void SetUp()
        {
            _dateTimeProvider = new Mock<IDateTimeProvider>();
            _uuidProvider = new Mock<IUniqueIdentifierProvider>();
            _blobClient = new Mock<IBlobClient>();
            _blob = new Mock<ICloudBlob>();
            
            _service = new BatchUploadService( _uuidProvider.Object,
                _dateTimeProvider.Object,
                _blobClient.Object,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync()
                }, Logger.None);

            _blobClient.Setup(_ => _.UploadFileAsync(It.IsAny<ICloudBlob>(),
                    It.IsAny<Stream>()))
                .Callback<ICloudBlob, Stream>((blob,
                    data) => _uploadedBytes = data.ReadAllBytes());
        }

        [TestMethod]
        public void GuardsAgainstMissingRequest()
        {
            Func<IActionResult> invocation = () => WhenTheBatchIsUploaded(null)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("Stream");
        }
        

        [TestMethod]
        public void GuardsAgainstMissingStreamOnTheRequest()
        {
            Func<IActionResult> invocation = () => WhenTheBatchIsUploaded(NewBatchUploadRequest())
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("Stream");   
        }

        [TestMethod]
        public async Task UploadsSuppliedStreamToBlobStoreWithANewBatchIdAndRespondsWithSaasUrlAndBatchId()
        {
            byte[] expectedUpload = NewRandomBytes();
            
            string expectedSaasUrl = NewRandomString();
            string expectedBatchId = NewRandomString();

            DateTimeOffset utcNow = NewRandomDate();

            BatchUploadBlobName blobName = new BatchUploadBlobName(expectedBatchId);
            
            GivenTheNewBatchId(expectedBatchId);
            AndTheBlobIsCreated(blobName);
            AndTheCurrentDateTime(utcNow);
            AndTheSaasUrl(blobName, utcNow, expectedSaasUrl);
            
            OkObjectResult result = await WhenTheBatchIsUploaded(NewBatchUploadRequest(_ => _.WithStream(expectedUpload))) as OkObjectResult;
            
            result?
                .Value
                .Should()
                .BeEquivalentTo(new BatchUploadResponse
                {
                    Url = expectedSaasUrl,
                    BatchId = expectedBatchId
                });
            
            AndTheStreamWasUploaded(expectedUpload);
        }

        private void AndTheStreamWasUploaded(byte[] stream)
            => _blobClient.Verify(_ => _.UploadFileAsync(_blob.Object,
                It.Is<Stream>(st => _uploadedBytes.SequenceEqual(stream))), 
                Times.Once());

        private async Task<IActionResult> WhenTheBatchIsUploaded(BatchUploadRequest uploadRequest)
            => await _service.UploadBatch(uploadRequest);

        private void GivenTheNewBatchId(string batchId)
            => _uuidProvider.Setup(_ => _.CreateUniqueIdentifier()).Returns(batchId);

        private void AndTheBlobIsCreated(string blobName)
            => _blobClient.Setup(_ => _.GetBlockBlobReference(blobName, ContainerName))
                .Returns(_blob.Object);

        private void AndTheSaasUrl(string blobName,
            DateTimeOffset utcNow,
            string saasUrl)
            => _blobClient.Setup(_ => _.GetBlobSasUrl(It.Is<string>(bn => bn == blobName),
                    It.Is<DateTimeOffset>(dt => dt == utcNow.AddHours(24)),
                    SharedAccessBlobPermissions.Read,
                    ContainerName))
                .Returns(saasUrl);

        private void AndTheCurrentDateTime(DateTimeOffset utcNow)
            => _dateTimeProvider.Setup(_ => _.UtcNow)
                .Returns(utcNow);

        private BatchUploadRequest NewBatchUploadRequest(Action<BatchUploadRequestBuilder> setUp = null)
        {
            BatchUploadRequestBuilder batchUploadRequestBuilder = new BatchUploadRequestBuilder();

            setUp?.Invoke(batchUploadRequestBuilder);
            
            return batchUploadRequestBuilder.Build();
        }
        
        private string NewRandomString() => new RandomString();

        private byte[] NewRandomBytes() => new RandomBytes();

        private DateTimeOffset NewRandomDate() => new RandomDateTime();
    }

    [TestClass]
    public class BatchUploadQueryServiceTests
    {
        private Mock<IBlobClient> _blobClient;

        private BatchUploadQueryService _service;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = new Mock<IBlobClient>();
            
            _service = new BatchUploadQueryService(_blobClient.Object,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync()
                }, 
                Logger.None);
        }

        [TestMethod]
        public void GuardsAgainstNoBatchIdBeingSupplied()
        {
            Func<IActionResult> invocation = () => WhenThenTheBatchIsQueried(null)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("batchId");
        }

        [TestMethod]
        public async Task ReturnsNotFoundIfNoStreamFoundForBatchId()
        {
            NotFoundResult notFoundResult = await WhenThenTheBatchIsQueried(NewRandomString()) as NotFoundResult;

            notFoundResult
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task ReturnsStringArraySavedToBlobStorageForBatchId()
        {
            string[] blobContents = new []
            {
                NewRandomString(),
                NewRandomString()
            };
            
            await using MemoryStream blobStream = new MemoryStream(blobContents.AsJsonBytes());

            string batchId = NewRandomString();
            
            GivenTheBlobContentsForBatchId(batchId, blobStream);
            
            OkObjectResult result = await WhenThenTheBatchIsQueried(batchId) as OkObjectResult;
            
            result?
                .Value
                .Should()
                .BeEquivalentTo(blobContents);
        }
        
        private async Task<IActionResult> WhenThenTheBatchIsQueried(string batchId)
            => await _service.GetBatchProviderIds(batchId);

        private void GivenTheBlobContentsForBatchId(string batchId,
            Stream stream)
        {
            Mock<ICloudBlob> cloudBlob = new Mock<ICloudBlob>();

            _blobClient.Setup(_ => _.GetBlobReferenceFromServerAsync(new BatchUploadProviderIdsBlobName(batchId),
                    "batchuploads"))
                .ReturnsAsync(cloudBlob.Object);
            _blobClient.Setup(_ => _.DownloadToStreamAsync(cloudBlob.Object))
                .ReturnsAsync(stream);
        }
        
        private string NewRandomString() => new RandomString();
    }
}