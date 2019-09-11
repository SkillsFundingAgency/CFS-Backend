using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

namespace CalculateFunding.Api.External.UnitTests.Version3.Services
{
    [TestClass]
    public class ProviderFundingVersionServiceTests
    {
        private const string providerFundingVersion = "id1";

        private readonly string blobName = $"{providerFundingVersion}.json";

        [TestMethod]
        public async Task GetProviderFundingVersionsBody_GivenNullOrEmptyId_ReturnsBadRequest()
        {
            //Arrange
            string id = "";

            ProviderFundingVersionService service = CreateProviderFundingVersionService();

            //Act
            IActionResult result = await service.GetProviderFundingVersion(id);

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
        public async Task GetProviderFundingVersionsBody_GivenBlobDoesNotExists_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();
            IBlobClient blobClient = CreateBlobClient();

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger, blobClient);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(providerFundingVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Blob '{blobName}' does not exist."));
        }

        [TestMethod]
        public async Task GetProviderFundingVersionsBody_GivenGetBlobReferenceCausesException_LogsAndReturnsInternalServerError()
        {
            //Arrange
            ILogger logger = CreateLogger();
            IBlobClient blobClient = CreateBlobClient();

            blobClient
                .BlobExistsAsync(blobName)
                .Returns(true);

            blobClient
                .GetBlobReferenceFromServerAsync(blobName)
                .Throws(new Exception());

            string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger, blobClient);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(providerFundingVersion);

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
        public async Task GetProviderFundingVersionsBody_GivenReturnsFromFileSystemCacheAndSkipBlobClientFetch_ReturnsOK()
        {
            //Arrange
            string template = "just a string";

            byte[] bytes = Encoding.UTF8.GetBytes(template);

            Stream memoryStream = new MemoryStream(bytes);

            ILogger logger = CreateLogger();
            IFileSystemCache fileSystemCache = CreateFileSystemCache();
            IBlobClient blobClient = CreateBlobClient();

            fileSystemCache.Exists(Arg.Is<ProviderFileSystemCacheKey>(
                    _ => _.Key == providerFundingVersion))
                .Returns(true);

            fileSystemCache.Get(Arg.Is<ProviderFileSystemCacheKey>(
                    _ => _.Key == providerFundingVersion))
                .Returns(memoryStream);

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger, blobClient, fileSystemCache);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(providerFundingVersion);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>();

            ContentResult contentResult = result as ContentResult;

            contentResult
                .ContentType
                .Should()
                .Be("application/json");

            contentResult
                .StatusCode
                .Should()
                .Be((int) HttpStatusCode.OK);

            contentResult
                .Content
                .Should()
                .Be(template);

            await blobClient
                .Received(0)
                .BlobExistsAsync(providerFundingVersion);

            await blobClient
                .Received(0)
                .GetAsync(Arg.Any<string>());

            fileSystemCache
                .Received(0)
                .Add(Arg.Is<ProviderFileSystemCacheKey>(_ => _.Key == providerFundingVersion),
                    memoryStream,
                    CancellationToken.None);
        }
        
        [TestMethod]
        public async Task GetProviderFundingVersionsBody_GivenReturnsFileSystemIsDisabledGetsFromBlobClientFetch_ReturnsOK()
        {
            //Arrange
            string template = "just a string";

            byte[] bytes = Encoding.UTF8.GetBytes(template);

            Stream memoryStream = new MemoryStream(bytes);

            ILogger logger = CreateLogger();
            IFileSystemCache fileSystemCache = CreateFileSystemCache();
            IBlobClient blobClient = CreateBlobClient();

            fileSystemCache.Exists(Arg.Is<ProviderFileSystemCacheKey>(
                    _ => _.Key == providerFundingVersion))
                .Returns(true);

            fileSystemCache.Get(Arg.Is<ProviderFileSystemCacheKey>(
                    _ => _.Key == providerFundingVersion))
                .Returns(memoryStream);

            ICloudBlob cloudBlob = CreateBlob();
            
            blobClient
                .BlobExistsAsync(blobName)
                .Returns(true);

            blobClient
                .GetBlobReferenceFromServerAsync(blobName)
                .Returns(cloudBlob);

            blobClient
                .DownloadToStreamAsync(cloudBlob)
                .Returns(memoryStream);

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger, blobClient, fileSystemCache, 
                Substitute.For<IExternalApiFileSystemCacheSettings>());

            //Act
            IActionResult result = await service.GetProviderFundingVersion(providerFundingVersion);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>();

            ContentResult contentResult = result as ContentResult;

            contentResult
                .ContentType
                .Should()
                .Be("application/json");

            contentResult
                .StatusCode
                .Should()
                .Be((int) HttpStatusCode.OK);

            contentResult
                .Content
                .Should()
                .Be(template);

            fileSystemCache
                .Received(0)
                .Exists(Arg.Any<FileSystemCacheKey>());

            fileSystemCache
                .Received(0)
                .Get(Arg.Any<FileSystemCacheKey>());

            fileSystemCache
                .Received(0)
                .Add(Arg.Is<ProviderFileSystemCacheKey>(_ => _.Key == providerFundingVersion),
                    memoryStream,
                    CancellationToken.None);
        }

        [TestMethod]
        [DataRow(true, 1)]
        [DataRow(false, 0)]
        public async Task GetProviderFundingVersionsBody_GivenReturnsFromBlobStorage_ReturnsOK(bool isFileSystemCacheEnabled, 
            int expectedCacheAddCount)
        {
            //Arrange
            string template = "just a string";

            byte[] bytes = Encoding.UTF8.GetBytes(template);

            Stream memoryStream = new MemoryStream(bytes);

            ILogger logger = CreateLogger();
            ICloudBlob cloudBlob = CreateBlob();
            IFileSystemCache fileSystemCache = CreateFileSystemCache();
            IExternalApiFileSystemCacheSettings cacheSettings = Substitute.For<IExternalApiFileSystemCacheSettings>();
            cacheSettings.IsEnabled.Returns(isFileSystemCacheEnabled);
            
            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .BlobExistsAsync(blobName)
                .Returns(true);

            blobClient
                .GetBlobReferenceFromServerAsync(blobName)
                .Returns(cloudBlob);

            blobClient
                .DownloadToStreamAsync(cloudBlob)
                .Returns(memoryStream);

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger, blobClient, fileSystemCache, cacheSettings);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(providerFundingVersion);

            //Assert
            result
                .Should()
                .BeOfType<ContentResult>();

            ContentResult contentResult = result as ContentResult;

            contentResult
                .ContentType
                .Should()
                .Be("application/json");

            contentResult
                .StatusCode
                .Should()
                .Be((int) HttpStatusCode.OK);

            contentResult
                .Content
                .Should()
                .Be(template);

            fileSystemCache
                .Received(expectedCacheAddCount)
                .Add(Arg.Is<ProviderFileSystemCacheKey>(_ => _.Key == providerFundingVersion),
                    memoryStream,
                    CancellationToken.None);
        }

        [TestMethod]
        [DataRow(false, "No")]
        [DataRow(false, "Yes")]
        [DataRow(true, "OK")]
        [DataRow(true, "Perhaps")]
        [DataRow(true, "OK")]
        [DataRow(false, "Perhaps")]
        public async Task IsHealthOk_ReturnsAsExpected(bool blobOk, string blobMessage)
        {
            //Arrange
            IBlobClient blobClient = Substitute.For<IBlobClient>();
            blobClient
                .IsHealthOk()
                .Returns((blobOk, blobMessage));

            ProviderFundingVersionService providerFundingVersionService = CreateProviderFundingVersionService(blobClient: blobClient);

            ServiceHealth health = await providerFundingVersionService.IsHealthOk();

            health.Name
                .Should()
                .Be(nameof(ProviderFundingVersionService));

            health.Dependencies.Count.Should().Be(1);

            health
                .Dependencies
                .Count(x => x.HealthOk == blobOk && x.Message == blobMessage)
                .Should()
                .Be(1);
        }

        private static ProviderFundingVersionService CreateProviderFundingVersionService(ILogger logger = null,
            IBlobClient blobClient = null,
            IFileSystemCache fileSystemCache = null,
            IExternalApiFileSystemCacheSettings cacheSettings = null)
        {
            return new ProviderFundingVersionService(blobClient ?? CreateBlobClient(),
                logger ?? CreateLogger(),
                ExternalApiResilienceTestHelper.GenerateTestPolicies(),
                fileSystemCache ?? CreateFileSystemCache(),
                cacheSettings ?? CreateFileSystemCacheSettings());
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

        private static IFileSystemCache CreateFileSystemCache()
        {
            return Substitute.For<IFileSystemCache>();
        }

        private static IExternalApiFileSystemCacheSettings CreateFileSystemCacheSettings()
        {
            IExternalApiFileSystemCacheSettings externalApiFileSystemCacheSettings
                = Substitute.For<IExternalApiFileSystemCacheSettings>();

            externalApiFileSystemCacheSettings.IsEnabled.Returns(true);

            return externalApiFileSystemCacheSettings;
        }
    }
}