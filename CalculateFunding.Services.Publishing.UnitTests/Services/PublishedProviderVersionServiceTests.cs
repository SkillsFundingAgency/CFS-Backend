using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishedProviderVersionServiceTests
    {
        private const string publishedProviderVersionId = "id1";

        private string blobName = $"{publishedProviderVersionId}.json";

        private const string body = "just a string";

        [TestMethod]
        public async Task GetPublishedProviderVersionBody_GivenNullOrEmptyId_ReturnsBadRequest()
        {
            //Arrange
            string id = "";

            PublishedProviderVersionService service = CreatePublishedProviderVersionService();

            //Act
            IActionResult result = await service.GetPublishedProviderVersionBody(id);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty id provided.");
        }

        [TestMethod]
        public async Task GetPublishedProviderVersionBody_GivenBlobDoesNotExists_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .BlobExistsAsync(Arg.Is(blobName))
                .Returns(false);

            PublishedProviderVersionService service = CreatePublishedProviderVersionService(logger, blobClient);

            //Act
            IActionResult result = await service.GetPublishedProviderVersionBody(publishedProviderVersionId);

            //Assert
            result
                .Should()
                .BeAssignableTo<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Blob '{blobName}' does not exist."));  
        }

        [TestMethod]
        public async Task GetPublishedProviderVersionBody_GivenGetBlobReferenceCausesException_LogsAndReturnsInternalServerError()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .BlobExistsAsync(Arg.Is(blobName))
                .Returns(true);

            blobClient
                .When(x => x.GetBlobReferenceFromServerAsync(Arg.Is(blobName)))
                .Do(x => { throw new Exception(); });

            string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

            PublishedProviderVersionService service = CreatePublishedProviderVersionService(logger, blobClient);

            //Act
            IActionResult result = await service.GetPublishedProviderVersionBody(publishedProviderVersionId);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task GetPublishedProviderVersionBody_GivenReturnsFromBlobStorage_returnsOK()
        {
            //Arrange
            byte[] bytes = Encoding.UTF8.GetBytes(body);

            Stream memoryStream = new MemoryStream(bytes);

            ILogger logger = CreateLogger();

            ICloudBlob cloudBlob = CreateBlob();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .BlobExistsAsync(Arg.Is(blobName))
                .Returns(true);

            blobClient
                 .GetBlobReferenceFromServerAsync(Arg.Is(blobName))
                 .Returns(cloudBlob);

            blobClient
                  .DownloadToStreamAsync(Arg.Is(cloudBlob))
                  .Returns(memoryStream);

            PublishedProviderVersionService service = CreatePublishedProviderVersionService(logger, blobClient);

            //Act
            IActionResult result = await service.GetPublishedProviderVersionBody(publishedProviderVersionId);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(body);
        }

        [TestMethod]
        public void SavePublishedProviderVersionBody_GivenGetBlobReferenceFromServerAsyncThrowsException_ThrowsNewException()
        {
            //Arrange
            IBlobClient blobClient = CreateBlobClient();
           
            blobClient
                .When(x => x.GetBlockBlobReference(Arg.Is(blobName)))
                .Do(x => { throw new Exception("Failed to get blob reference"); });

            ILogger logger = CreateLogger();

            string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

            PublishedProviderVersionService service = CreatePublishedProviderVersionService(logger, blobClient);

            //Act
            Func<Task> test = async () => await service.SavePublishedProviderVersionBody(publishedProviderVersionId, body);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .And
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is<Exception>(m => m.Message == "Failed to get blob reference"), Arg.Is(errorMessage));
        }

        [TestMethod]
        public void SavePublishedProviderVersionBody_GivenUoloadAsyncThrowsException_ThrowsNewException()
        {
            //Arrange
            IBlobClient blobClient = CreateBlobClient();

            blobClient
                .When(x => x.UploadAsync(Arg.Any<ICloudBlob>(), Arg.Is(body)))
                .Do(x => { throw new Exception("Failed to upload blob"); });

            ILogger logger = CreateLogger();

            string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

            PublishedProviderVersionService service = CreatePublishedProviderVersionService(logger, blobClient);

            //Act
            Func<Task> test = async () => await service.SavePublishedProviderVersionBody(publishedProviderVersionId, body);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .And
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is<Exception>(m => m.Message == "Failed to upload blob"), Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task SavePublishedProviderVersionBody_GivenUoloadAsyncSuccessful_EnsuresCalledWithCorrectParameters()
        {
            //Arrange
            ICloudBlob blob = Substitute.For<ICloudBlob>();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Is(blobName))
                .Returns(blob);

            PublishedProviderVersionService service = CreatePublishedProviderVersionService(blobClient: blobClient);

            //Act
            await service.SavePublishedProviderVersionBody(publishedProviderVersionId, body);

            //Assert
            await
                blobClient
                .Received(1)
                .UploadAsync(Arg.Is(blob), Arg.Is(body));
         }

        private static PublishedProviderVersionService CreatePublishedProviderVersionService(
                ILogger logger = null,
                IBlobClient blobClient = null)
        {
            return new PublishedProviderVersionService(
                logger ?? CreateLogger(),
                blobClient ?? CreateBlobClient(),
                PublishingResilienceTestHelper.GenerateTestPolicies());
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        private static ICloudBlob CreateBlob()
        {
            return Substitute.For<ICloudBlob>();
        }
    }
}
