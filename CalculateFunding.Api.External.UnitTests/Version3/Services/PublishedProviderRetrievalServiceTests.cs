using AutoMapper;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;
using Serilog;
using CalculateFunding.Api.External.V3.MappingProfiles;
using Microsoft.AspNetCore.Mvc;
using CalculateFunding.Tests.Common.Helpers;
using System;
using FluentAssertions;
using System.IO;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Services.Core.Extensions;
using ProvidersModels = CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Common.ApiClient.Models;
using System.Net;

namespace CalculateFunding.Api.External.UnitTests.Version3.Services
{
    [TestClass]
    public class PublishedProviderRetrievalServiceTests
    {
        private const string ProviderVersionFolderName = "providerVersion";

        private string _publishedProviderVersion;
        private string _providerId;
        private string _providerVersionId;

        private IMapper _mapper;
        private IPublishedFundingRepository _publishedFundingRepository;
        private IProvidersApiClient _providersApiClient;
        private ILogger _logger;
        private IExternalApiFileSystemCacheSettings _cacheSettings;
        private IFileSystemCache _fileSystemCache;

        private PublishedProviderRetrievalService _publishedProviderRetrievalService;


        [TestInitialize]
        public void Initialize()
        {
            _mapper = new MapperConfiguration(_ =>
            {
                _.AddProfile<ExternalServiceMappingProfile>();
            }).CreateMapper();

            _publishedProviderVersion = NewRandomString();
            _providerId = NewRandomString();
            _providerVersionId = NewRandomString();

            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _providersApiClient = Substitute.For<IProvidersApiClient>();
            _logger = Substitute.For<ILogger>();
            _cacheSettings = Substitute.For<IExternalApiFileSystemCacheSettings>();
            _fileSystemCache = Substitute.For<IFileSystemCache>();

            _publishedProviderRetrievalService = new PublishedProviderRetrievalService(
                _publishedFundingRepository,
                PublishingResilienceTestHelper.GenerateTestPolicies(),
                _providersApiClient ,
                _logger,
                _mapper,
                _cacheSettings ,
                _fileSystemCache);
        }

        [TestMethod]
        public void GetPublishedProviderInformation_PublishedProviderVersionIsNull_ThrowsArgumentException()
        {
            _publishedProviderVersion = null;

            Func<Task> test = WhenGetPublishedProviderInformation;

            test
               .Should()
               .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task GetPublishedProviderInformation_ExistsInCache_ReturnsCachedPublishedProvider()
        {
            GivenCacheSettings(true);
            AndFileSystemCacheExists(true);
            AndGetFileSystemCache();

            IActionResult actionResult = await WhenGetPublishedProviderInformation();

            ThenPublishedProviderInformationSucceed(actionResult);
        }

        [TestMethod]
        public async Task GetPublishedProviderInformation_GetPublishedProviderIdReturnsNull_ReturnsNotFoundResult()
        {
            _providerVersionId = null;

            GivenCacheSettings(false);
            AndGetPublishedProviderId();

            IActionResult actionResult = await WhenGetPublishedProviderInformation();

            ThenPublishedProviderInformationNotFound(actionResult);
        }

        [TestMethod]
        public async Task GetPublishedProviderInformation_GetProviderByIdFromProviderVersionReturnsNull_ReturnsInternalServerErrorResultResult()
        {
            GivenCacheSettings(false);
            AndGetPublishedProviderId();

            IActionResult actionResult = await WhenGetPublishedProviderInformation();

            ThenPublishedProviderInformationReturnsIntervalServerError(actionResult);
        }

        [TestMethod]
        public async Task GetPublishedProviderInformation_GetProviderByIdFromProviderVersion_ReturnsProviderVersionSearchResult()
        {
            AndGetPublishedProviderId();
            AndGetProviderByIdFromProviderVersion();

            IActionResult actionResult = await WhenGetPublishedProviderInformation();

            ThenPublishedProviderInformationSucceed(actionResult);
        }

        private void GivenCacheSettings(bool isEnabled)
        {
            _cacheSettings
                .IsEnabled
                .Returns(isEnabled);
        }

        private void AndFileSystemCacheExists(bool cacheExists)
        {
            _fileSystemCache
                .Exists(Arg.Is<FileSystemCacheKey>(_ => _.Path == $"{ProviderVersionFolderName}\\{_publishedProviderVersion}.json"))
                .Returns(cacheExists);
        }

        private void AndGetFileSystemCache()
        {
            ProviderVersionSearchResult providerVersionSearchResult = NewProviderVersionSearchResult(_ => _.WithProviderId(_providerId));

            _fileSystemCache
                .Get(Arg.Is<FileSystemCacheKey>(_ => _.Path == $"{ProviderVersionFolderName}\\{_publishedProviderVersion}.json"))
                .Returns(new MemoryStream(providerVersionSearchResult.AsJsonBytes()));
        }

        private void AndGetPublishedProviderId()
        {
            _publishedFundingRepository
                .GetPublishedProviderId(_publishedProviderVersion)
                .Returns((providerVersionId: _providerVersionId, providerId: _providerId));
        }

        private void AndGetProviderByIdFromProviderVersion()
        {
            _providersApiClient
                .GetProviderByIdFromProviderVersion(_providerVersionId, _providerId)
                .Returns(new ApiResponse<ProvidersModels.ProviderVersionSearchResult>(
                    HttpStatusCode.OK, 
                    NewProvidersProviderVersionSearchResultBuilder(_ => _.WithProviderId(_providerId))));
        }

        private async Task<IActionResult> WhenGetPublishedProviderInformation()
        {
            return await _publishedProviderRetrievalService.GetPublishedProviderInformation(_publishedProviderVersion);
        }

        private void ThenPublishedProviderInformationSucceed(IActionResult actionResult)
        {
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            okObjectResult
                .Value
                .Should()
                .NotBeNull();

            okObjectResult
                .Value
                .Should()
                .BeOfType<ProviderVersionSearchResult>();

            ProviderVersionSearchResult providerVersionSearchResult = okObjectResult.Value as ProviderVersionSearchResult;

            providerVersionSearchResult
                .ProviderId
                .Should()
                .Be(_providerId);
        }

        private void ThenPublishedProviderInformationNotFound(IActionResult actionResult)
        {
            actionResult
                .Should()
                .BeOfType<NotFoundResult>();
        }

        private void ThenPublishedProviderInformationReturnsIntervalServerError(IActionResult actionResult)
        {
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>();
        }

        private ProviderVersionSearchResult NewProviderVersionSearchResult(Action<ProviderVersionSearchResultBuilder> setUp = null)
        {
            ProviderVersionSearchResultBuilder providerVersionSearchResultBuilder = new ProviderVersionSearchResultBuilder();

            setUp?.Invoke(providerVersionSearchResultBuilder);

            return providerVersionSearchResultBuilder.Build();
        }

        private ProvidersModels.ProviderVersionSearchResult NewProvidersProviderVersionSearchResultBuilder(Action<ProvidersProviderVersionSearchResultBuilder> setUp = null)
        {
            ProvidersProviderVersionSearchResultBuilder providersProviderVersionSearchResultBuilder = new ProvidersProviderVersionSearchResultBuilder();

            setUp?.Invoke(providersProviderVersionSearchResultBuilder);

            return providersProviderVersionSearchResultBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
