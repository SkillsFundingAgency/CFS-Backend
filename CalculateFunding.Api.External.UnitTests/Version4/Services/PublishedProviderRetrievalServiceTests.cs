using AutoMapper;
using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.Services;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;
using Serilog;
using CalculateFunding.Api.External.V4.MappingProfiles;
using Microsoft.AspNetCore.Mvc;
using CalculateFunding.Tests.Common.Helpers;
using System;
using FluentAssertions;
using System.IO;
using CalculateFunding.Api.External.V4.Models;
using CalculateFunding.Services.Core.Extensions;
using ProvidersModels = CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Common.ApiClient.Models;
using System.Net;
using System.Threading;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;

namespace CalculateFunding.Api.External.UnitTests.Version4.Services
{
    [TestClass]
    public class PublishedProviderRetrievalServiceTests
    {
        private const string ProviderVersionFolderName = "providerVersion";

        private string _channel;
        private string _channelCode;
        private int _channelId;
        private string _publishedProviderVersion;
        private string _corePublishedProviderVersion;
        private string _providerId;

        private IMapper _mapper;
        private IReleaseManagementRepository _releaseManagementRepository;
        private IProvidersApiClient _providersApiClient;
        private ILogger _logger;
        private IExternalApiFileSystemCacheSettings _cacheSettings;
        private IFileSystemCache _fileSystemCache;
        private IChannelUrlToChannelResolver _channelUrlToChannelResolver;

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
            _corePublishedProviderVersion = NewRandomString();
            _channel = NewRandomString();
            _channelCode = NewRandomString();
            _channelId = new RandomNumberBetween(1, 5);

            _releaseManagementRepository = Substitute.For<IReleaseManagementRepository>();
            _providersApiClient = Substitute.For<IProvidersApiClient>();
            _logger = Substitute.For<ILogger>();
            _cacheSettings = Substitute.For<IExternalApiFileSystemCacheSettings>();
            _fileSystemCache = Substitute.For<IFileSystemCache>();
            _channelUrlToChannelResolver = Substitute.For<IChannelUrlToChannelResolver>();

            _publishedProviderRetrievalService = new PublishedProviderRetrievalService(
                _releaseManagementRepository,
                PublishingResilienceTestHelper.GenerateTestPolicies(),
                _providersApiClient ,
                _logger,
                _mapper,
                _cacheSettings,
                _channelUrlToChannelResolver,
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
            AndGetChannelFromResolver();
            AndFileSystemCacheExists(true);
            AndGetFileSystemCache();

            ActionResult<ProviderVersionSearchResult> actionResult = await WhenGetPublishedProviderInformation();

            ThenPublishedProviderInformationSucceed(actionResult);
        }

        [TestMethod]
        public async Task GetPublishedProviderInformation_GetPublishedProviderIdReturnsNull_ReturnsNotFoundResult()
        {
            GivenCacheSettings(false);
            AndGetChannelFromResolver();

            ActionResult<ProviderVersionSearchResult> actionResult = await WhenGetPublishedProviderInformation();

            ThenPublishedProviderInformationNotFound(actionResult);
        }

        [TestMethod]
        public async Task GetPublishedProviderInformation_GetProviderByIdFromProviderVersionReturnsNull_ReturnsInternalServerErrorResultResult()
        {
            GivenCacheSettings(false);
            AndGetChannelFromResolver();
            AndGetPublishedProviderId();

            ActionResult<ProviderVersionSearchResult> actionResult = await WhenGetPublishedProviderInformation();

            ThenPublishedProviderInformationReturnsIntervalServerError(actionResult);
        }

        [TestMethod]
        public async Task GetPublishedProviderInformation_GetProviderByIdFromProviderVersion_ReturnsProviderVersionSearchResult()
        {
            GivenCacheSettings(true);
            AndGetChannelFromResolver();
            AndGetPublishedProviderId();
            AndGetProviderByIdFromProviderVersion();

            ActionResult<ProviderVersionSearchResult> actionResult = await WhenGetPublishedProviderInformation();

            ThenPublishedProviderInformationSucceed(actionResult);
            AndTheFileSystemCacheFolderWasEnsuredToExist();
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
                .Exists(Arg.Is<FileSystemCacheKey>(_ => _.Path == $"{ProviderVersionFolderName}\\{_channelCode}_{_publishedProviderVersion}"))
                .Returns(cacheExists);
        }

        private void AndGetFileSystemCache()
        {
            ProviderVersionSearchResult providerVersionSearchResult = NewProviderVersionSearchResult(_ => _.WithProviderId(_providerId));

            _fileSystemCache
                .Get(Arg.Is<FileSystemCacheKey>(_ => _.Path == $"{ProviderVersionFolderName}\\{_channelCode}_{_publishedProviderVersion}"))
                .Returns(new MemoryStream(providerVersionSearchResult.AsJsonBytes()));
        }

        private void AndGetChannelFromResolver()
        {
            _channelUrlToChannelResolver
                .ResolveUrlToChannel(_channel)
                .Returns(new Channel { ChannelId = _channelId, ChannelCode = _channelCode, UrlKey = _channel });
        }

        private void AndGetPublishedProviderId()
        {
            _releaseManagementRepository
                .GetReleasedProvider(_publishedProviderVersion, _channelId)
                .Returns(new ProviderVersionInChannel {
                    ChannelName = _channel,
                    ChannelId = _channelId,
                    CoreProviderVersionId = _corePublishedProviderVersion,
                    ProviderId = _providerId
                } );
        }

        private void AndGetProviderByIdFromProviderVersion()
        {
            _providersApiClient
                .GetProviderByIdFromProviderVersion(_corePublishedProviderVersion, _providerId)
                .Returns(new ApiResponse<ProvidersModels.ProviderVersionSearchResult>(
                    HttpStatusCode.OK, 
                    NewProvidersProviderVersionSearchResultBuilder(_ => _.WithProviderId(_providerId))));
        }

        private async Task<ActionResult<ProviderVersionSearchResult>> WhenGetPublishedProviderInformation()
        {
            return await _publishedProviderRetrievalService.GetPublishedProviderInformation(_channel, _publishedProviderVersion);
        }

        private void AndTheFileSystemCacheFolderWasEnsuredToExist()
        {
            _fileSystemCache
                .Received(1)
                .Add(Arg.Any<FileSystemCacheKey>(), 
                    Arg.Any<Stream>(), 
                    Arg.Is(CancellationToken.None),  
                    Arg.Is(true));
        }
        
        private void ThenPublishedProviderInformationSucceed(ActionResult<ProviderVersionSearchResult> actionResult)
        {
            actionResult
                .Value
                .Should()
                .NotBeNull();

            actionResult
                .Value
                .Should()
                .BeOfType<ProviderVersionSearchResult>();

            ProviderVersionSearchResult providerVersionSearchResult = actionResult.Value;

            providerVersionSearchResult
                .ProviderId
                .Should()
                .Be(_providerId);
        }

        private void ThenPublishedProviderInformationNotFound(ActionResult<ProviderVersionSearchResult> actionResult)
        {
            actionResult
                .Result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        private void ThenPublishedProviderInformationReturnsIntervalServerError(ActionResult<ProviderVersionSearchResult> actionResult)
        {
            actionResult
                .Result
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
