using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.Services;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using FundingManagementInterfaces = CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using System.Collections.Generic;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;

namespace CalculateFunding.Api.External.UnitTests.Version4.Services
{
    [TestClass]
    public class ProviderFundingVersionServiceTests
    {
        private const string providerFundingVersion = "id1";
        private const string publishedProviderVersion = "publishedProviderVersion";
        private const string channelUrlKey = "contracts";
        private const string channelCode = "Contracting";
        private const int channelId = 1234;
        private readonly string blobName = $"{channelCode}/{providerFundingVersion}.json";

        [TestMethod]
        public async Task GetFundings_GivenPublishedProviderVersionIncludedInFunding_ReturnsFundingIds()
        {
            //Arrange
            IEnumerable<string> fundingGroups = new[] { "id1" };

            FundingManagementInterfaces.IReleaseManagementRepository releaseManagementRepository = Substitute.For<FundingManagementInterfaces.IReleaseManagementRepository>();

            IChannelUrlToChannelResolver channelUrlToChannelResolver = Substitute.For<IChannelUrlToChannelResolver>();

            ProviderFundingVersionService service = CreateProviderFundingVersionService(releaseManagementRepository: releaseManagementRepository,
                    channelUrlToChannelResolver: channelUrlToChannelResolver);

            channelUrlToChannelResolver.ResolveUrlToChannel(Arg.Is(channelUrlKey)).Returns(new Channel { ChannelCode = channelCode, ChannelId = channelId });

            releaseManagementRepository.GetFundingGroupIdsForProviderFunding(Arg.Is(channelId), Arg.Is<string>(publishedProviderVersion))
                .Returns(fundingGroups);

            //Act
            IActionResult result = await service.GetFundings(channelUrlKey, publishedProviderVersion);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult contentResult = result as OkObjectResult;

            IEnumerable<dynamic> fundingIdResults = contentResult.Value as IEnumerable<dynamic>;

            fundingIdResults.Should()
                .BeEquivalentTo(fundingGroups);
        }

        [TestMethod]
        public async Task GetProviderFundingVersionsBody_GivenNullOrEmptyId_ReturnsBadRequest()
        {
            //Arrange
            string id = "";

            ProviderFundingVersionService service = CreateProviderFundingVersionService();

            //Act
            IActionResult result = await service.GetProviderFundingVersion(channelUrlKey, id);

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

            IChannelUrlToChannelResolver channelUrlToChannelResolver = Substitute.For<IChannelUrlToChannelResolver>();

            channelUrlToChannelResolver.ResolveUrlToChannel(channelUrlKey).Returns(new Channel { ChannelCode = channelCode, ChannelId = channelId });

            FundingManagementInterfaces.IReleaseManagementRepository releaseManagementRepository = Substitute.For<FundingManagementInterfaces.IReleaseManagementRepository>();

            releaseManagementRepository.ContainsProviderVersion(channelId, providerFundingVersion).Returns(true);
            
            IBlobDocumentPathGenerator blobDocumentPathGenerator = new BlobDocumentPathGenerator();

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger, 
                blobClient, 
                channelUrlToChannelResolver: channelUrlToChannelResolver, 
                releaseManagementRepository: releaseManagementRepository,
                blobDocumentPathGenerator: blobDocumentPathGenerator);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(channelUrlKey,
                providerFundingVersion);

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

            IChannelUrlToChannelResolver channelUrlToChannelResolver = Substitute.For<IChannelUrlToChannelResolver>();

            channelUrlToChannelResolver.ResolveUrlToChannel(channelUrlKey).Returns(new Channel { ChannelCode = channelCode, ChannelId = channelId });

            FundingManagementInterfaces.IReleaseManagementRepository releaseManagementRepository = Substitute.For<FundingManagementInterfaces.IReleaseManagementRepository>();

            releaseManagementRepository.ContainsProviderVersion(channelId, providerFundingVersion).Returns(true);

            IBlobDocumentPathGenerator blobDocumentPathGenerator = new BlobDocumentPathGenerator();

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger, 
                blobClient,
                channelUrlToChannelResolver: channelUrlToChannelResolver, 
                releaseManagementRepository: releaseManagementRepository,
                blobDocumentPathGenerator: blobDocumentPathGenerator);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(channelUrlKey, providerFundingVersion);

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

            fileSystemCache.Exists(Arg.Is<ProviderFundingFileSystemCacheKey>(
                    _ => _.Key == $"{channelCode}_{providerFundingVersion}"))
                .Returns(true);

            fileSystemCache.Get(Arg.Is<ProviderFundingFileSystemCacheKey>(
                    _ => _.Key == $"{channelCode}_{providerFundingVersion}"))
                .Returns(memoryStream);

            IChannelUrlToChannelResolver channelUrlToChannelResolver = Substitute.For<IChannelUrlToChannelResolver>();

            channelUrlToChannelResolver.ResolveUrlToChannel(channelUrlKey).Returns(new Channel { ChannelCode = channelCode, ChannelId = channelId });
           
            channelUrlToChannelResolver.GetContentWithChannelProviderVersion(memoryStream, channelCode).Returns(memoryStream);

            FundingManagementInterfaces.IReleaseManagementRepository releaseManagementRepository = Substitute.For<FundingManagementInterfaces.IReleaseManagementRepository>();

            releaseManagementRepository.ContainsProviderVersion(channelId, providerFundingVersion).Returns(true);
            
            IBlobDocumentPathGenerator blobDocumentPathGenerator = new BlobDocumentPathGenerator();

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger, 
                blobClient, fileSystemCache,
                channelUrlToChannelResolver: channelUrlToChannelResolver, 
                releaseManagementRepository: releaseManagementRepository,
                blobDocumentPathGenerator: blobDocumentPathGenerator);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(channelUrlKey, providerFundingVersion);

            //Assert
            result
                .Should()
                .BeOfType<FileStreamResult>();

            FileStreamResult fileStreamResult = result as FileStreamResult;

            fileStreamResult
                .ContentType
                .Should()
                .Be("application/json");

            await blobClient
                .Received(0)
                .BlobExistsAsync(providerFundingVersion);

            await blobClient
                .Received(0)
                .GetAsync(Arg.Any<string>());

            fileSystemCache
                .Received(0)
                .Add(Arg.Is<ProviderFundingFileSystemCacheKey>(_ => _.Key == providerFundingVersion),
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

            fileSystemCache.Exists(Arg.Is<ProviderFundingFileSystemCacheKey>(
                    _ => _.Key == providerFundingVersion))
                .Returns(true);

            fileSystemCache.Get(Arg.Is<ProviderFundingFileSystemCacheKey>(
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

            IChannelUrlToChannelResolver channelUrlToChannelResolver = Substitute.For<IChannelUrlToChannelResolver>();

            channelUrlToChannelResolver.ResolveUrlToChannel(channelUrlKey).Returns(new Channel { ChannelCode = channelCode, ChannelId = channelId });

            FundingManagementInterfaces.IReleaseManagementRepository releaseManagementRepository = Substitute.For<FundingManagementInterfaces.IReleaseManagementRepository>();

            releaseManagementRepository.ContainsProviderVersion(1234, providerFundingVersion).Returns(true);
            
            channelUrlToChannelResolver.GetContentWithChannelProviderVersion(memoryStream ,channelCode).Returns(memoryStream);

            IBlobDocumentPathGenerator blobDocumentPathGenerator = new BlobDocumentPathGenerator();

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger,
                blobClient,
                fileSystemCache, 
                Substitute.For<IExternalApiFileSystemCacheSettings>(),
                channelUrlToChannelResolver: channelUrlToChannelResolver,
                releaseManagementRepository: releaseManagementRepository,
                blobDocumentPathGenerator: blobDocumentPathGenerator);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(channelUrlKey, providerFundingVersion);

            //Assert
            result
                .Should()
                .BeOfType<FileStreamResult>();

            FileStreamResult fileStreamResult = result as FileStreamResult;

            fileStreamResult
                .ContentType
                .Should()
                .Be("application/json");

            fileSystemCache
                .Received(0)
                .Exists(Arg.Any<FileSystemCacheKey>());

            fileSystemCache
                .Received(0)
                .Get(Arg.Any<FileSystemCacheKey>());

            fileSystemCache
                .Received(0)
                .Add(Arg.Is<ProviderFundingFileSystemCacheKey>(_ => _.Key == providerFundingVersion),
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

            IChannelUrlToChannelResolver channelUrlToChannelResolver = Substitute.For<IChannelUrlToChannelResolver>();

            channelUrlToChannelResolver.ResolveUrlToChannel(channelUrlKey).Returns(new Channel { ChannelCode = channelCode, ChannelId = channelId });

            channelUrlToChannelResolver.GetContentWithChannelProviderVersion(memoryStream, channelCode).Returns(memoryStream);

            FundingManagementInterfaces.IReleaseManagementRepository releaseManagementRepository = Substitute.For<FundingManagementInterfaces.IReleaseManagementRepository>();

            releaseManagementRepository.ContainsProviderVersion(1234, providerFundingVersion).Returns(true);

            IBlobDocumentPathGenerator blobDocumentPathGenerator = new BlobDocumentPathGenerator();

            ProviderFundingVersionService service = CreateProviderFundingVersionService(logger,
                blobClient,
                fileSystemCache,
                cacheSettings,
                channelUrlToChannelResolver: channelUrlToChannelResolver,
                blobDocumentPathGenerator: blobDocumentPathGenerator,
                releaseManagementRepository: releaseManagementRepository);

            //Act
            IActionResult result = await service.GetProviderFundingVersion(channelUrlKey, providerFundingVersion);

            //Assert
            result
                .Should()
                .BeOfType<FileStreamResult>();

            FileStreamResult fileStreamResult = result as FileStreamResult;

            fileStreamResult
                .ContentType
                .Should()
                .Be("application/json");

            fileSystemCache
                .Received(expectedCacheAddCount)
                .Add(Arg.Is<ProviderFundingFileSystemCacheKey>(_ => _.Key == $"{channelCode}_{providerFundingVersion}"),
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
            IExternalApiFileSystemCacheSettings cacheSettings = null,
            FundingManagementInterfaces.IReleaseManagementRepository releaseManagementRepository = null,
            IBlobDocumentPathGenerator blobDocumentPathGenerator = null,
            IChannelUrlToChannelResolver channelUrlToChannelResolver = null)
        {
            return new ProviderFundingVersionService(blobClient ?? CreateBlobClient(),
                releaseManagementRepository ?? CreateReleaseManagementRepository(),
                channelUrlToChannelResolver ?? CreateChannelUrlToChannelResolver(),
                blobDocumentPathGenerator ?? CreateBlobDocumentPathGenerator(),
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
        private static IBlobDocumentPathGenerator CreateBlobDocumentPathGenerator()
        {
            return Substitute.For<IBlobDocumentPathGenerator>();
        }

        private static FundingManagementInterfaces.IReleaseManagementRepository CreateReleaseManagementRepository()
        {
            return Substitute.For<FundingManagementInterfaces.IReleaseManagementRepository>();
        }

        private static IChannelUrlToChannelResolver CreateChannelUrlToChannelResolver()
        {
            return Substitute.For<IChannelUrlToChannelResolver>();
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