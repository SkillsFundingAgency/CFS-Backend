using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class PublishedProviderChannelVersionServiceTests
    {
        private const string publishedProviderVersionId = "id1";
        private const string publishedProviderSpecificationId = "specId1";
        private const string body = "just a string";
        private const string channelCode = "channel1";
        private const string ContainerName = "releasedproviders";

        private readonly string blobName = $"{channelCode}/{publishedProviderVersionId}.json";

        private PublishedProviderChannelVersionService _service;
        private IBlobClient _blobClient;
        private ILogger _logger;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = Substitute.For<IBlobClient>();
            _logger = Substitute.For<ILogger>();

            _service = new PublishedProviderChannelVersionService(_logger,
                _blobClient,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                });
        }

        [TestMethod]
        public void SavePublishedProviderVersionBody_GivenGetBlobReferenceFromServerAsyncThrowsException_ThrowsNewException()
        {
            // Arrange
            _blobClient
                .When(x => x.GetBlockBlobReference(Arg.Is(blobName)))
                .Do(x => { throw new Exception("Failed to get blob reference"); });

            string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

            //Act
            Func<Task> test = async () => await _service.SavePublishedProviderVersionBody(publishedProviderVersionId, body, publishedProviderSpecificationId, channelCode);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .And
                .Message
                .Should()
                .Be(errorMessage);

            _logger
                .Received(1)
                .Error(Arg.Is<Exception>(m => m.Message == "Failed to get blob reference"), Arg.Is(errorMessage));
        }

        [TestMethod]
        public void SavePublishedProviderVersionBody_GivenUploadAsyncThrowsException_ThrowsNewException()
        {
            //Arrange
            _blobClient
                .When(x => x.UploadFileAsync(Arg.Any<string>(), Arg.Is(body), Arg.Is(ContainerName)))
                .Do(x => { throw new Exception("Failed to upload blob"); });

            string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

            //Act
            Func<Task> test = async () => await _service.SavePublishedProviderVersionBody(publishedProviderVersionId, body, publishedProviderSpecificationId, channelCode);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .And
                .Message
                .Should()
                .Be(errorMessage);

            _logger
                .Received(1)
                .Error(Arg.Is<Exception>(m => m.Message == "Failed to upload blob"), Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task SavePublishedProviderVersionBody_GivenUploadAsyncSuccessful_EnsuresCalledWithCorrectParameters()
        {
            //Arrange
            ICloudBlob blob = Substitute.For<ICloudBlob>();

            _blobClient
                .GetBlockBlobReference(Arg.Is(blobName))
                .Returns(blob);

            //Act
            await _service.SavePublishedProviderVersionBody(publishedProviderVersionId, body, publishedProviderSpecificationId, channelCode);

            //Assert
            await
                _blobClient
                    .Received(1)
                    .UploadFileAsync(Arg.Any<string>(), Arg.Is(body), Arg.Is(ContainerName));
            await
                _blobClient
                    .Received(1)
                    .AddMetadataAsync(
                        Arg.Is(blob),
                        Arg.Is<IDictionary<string, string>>(_ => _.ContainsKey("specification-id") && _["specification-id"] == publishedProviderSpecificationId));
        }
    }
}
