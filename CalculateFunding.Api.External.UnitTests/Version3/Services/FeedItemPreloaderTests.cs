using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Models.External;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using StackExchange.Redis;

namespace CalculateFunding.Api.External.UnitTests.Version3.Services
{
    [TestClass]
    public class FeedItemPreloaderTests
    {
        private FeedItemPreLoader _preLoader;

        private FeedItemPreLoaderSettings _settings;
        private IPublishedFundingRetrievalService _retrievalService;
        private IExternalApiFileSystemCacheSettings _apiFileSystemCacheSettings;
        private IFundingFeedSearchService _searchService;
        private IFileSystemCache _cache;

        [TestInitialize]
        public void SetUp()
        {
            _settings = new FeedItemPreLoaderSettings();
            _retrievalService = Substitute.For<IPublishedFundingRetrievalService>();
            _searchService = Substitute.For<IFundingFeedSearchService>();
            _cache = Substitute.For<IFileSystemCache>();
            _apiFileSystemCacheSettings = Substitute.For<IExternalApiFileSystemCacheSettings>();

            _preLoader = new FeedItemPreLoader(_settings,
                _retrievalService,
                _searchService,
                _cache,
                _apiFileSystemCacheSettings);
            
            _retrievalService
                .GetFundingFeedDocument(Arg.Any<string>(), Arg.Any<bool>())
                .Returns((string)null);
        }

        [TestMethod]
        public void EnsureFoldersExistDelegatesToCache()
        {
             GivenTheSettings(1, 1, true, true);
             
            _preLoader.EnsureFoldersExists();
            
            _cache
                .Received(1)
                .EnsureFoldersExist(FundingFileSystemCacheKey.Folder, ProviderFundingFileSystemCacheKey.Folder);
        }
        
        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(false, false)]
        public void EnsureFoldersExistExitsEarlyIfShouldNotPreloadOrCachingDisabled(bool shouldPreLoad, 
            bool isFileSystemCacheEnabled)
        {
            GivenTheSettings(1, 1, shouldPreLoad, isFileSystemCacheEnabled);
            
            _preLoader.EnsureFoldersExists();
            
            _cache
                .DidNotReceive()
                .EnsureFoldersExist(FundingFileSystemCacheKey.Folder, ProviderFundingFileSystemCacheKey.Folder);
        }

        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(false, false)]
        public async Task ExitsEarlyIfSettingsShouldPreloadFalseOrCachingDisabled(bool shouldPreLoad, 
            bool isFileSystemCacheEnabled)
        {
            GivenTheSettings(1, 1, shouldPreLoad, isFileSystemCacheEnabled);

            await WhenThePreloadIsRun();

            ThenNoFeedItemsWerePreLoaded();
        }

        [TestMethod]
        public async Task PreLoadsLatestFundingItemDocumentsByPageSize_ExampleOne()
        {
            int pageSize = 20;
            int preloadCount = 50;

            SearchFeedV3<PublishedFundingIndex> pageOne = NewV3SearchFeed(_ => _.WithFeedItems(NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex()
            ));
            SearchFeedV3<PublishedFundingIndex> pageTwo = NewV3SearchFeed(_ => _.WithFeedItems(NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex()
            ));
            SearchFeedV3<PublishedFundingIndex> pageThree = NewV3SearchFeed(_ => _.WithFeedItems(NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex()
            ));

            GivenTheSettings(pageSize, preloadCount, true);
            AndTheSearchFeedPage(1, pageSize, pageOne);
            AndTheSearchFeedPage(2, pageSize, pageTwo);
            AndTheSearchFeedPage(3, pageSize, pageThree);

            await WhenThePreloadIsRun();

            ThenTheFeedItemDocumentsWerePreLoaded(pageOne.Entries.Select(_ => _.DocumentPath)
                .Concat(pageTwo.Entries.Select(_ => _.DocumentPath)
                    .Concat(pageThree.Entries.Select(_ => _.DocumentPath))));
        }

        [TestMethod]
        public async Task PreLoadsLatestFundingItemDocumentsByPageSize_ExampleTwo()
        {
            int pageSize = 10;
            int preloadCount = 15;

            SearchFeedV3<PublishedFundingIndex> pageOne = NewV3SearchFeed(_ => _.WithFeedItems(NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex()
            ));
            SearchFeedV3<PublishedFundingIndex> pageTwo = NewV3SearchFeed(_ => _.WithFeedItems(NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex(),
                NewPublishedFundingIndex()
            ));

            GivenTheSettings(pageSize, preloadCount, true);
            AndTheSearchFeedPage(1, pageSize, pageOne);
            AndTheSearchFeedPage(2, pageSize, pageTwo);

            await WhenThePreloadIsRun();

            ThenTheFeedItemDocumentsWerePreLoaded(pageOne.Entries.Select(_ => _.DocumentPath)
                .Concat(pageTwo.Entries.Select(_ => _.DocumentPath)));
        }

        private void ThenTheFeedItemDocumentsWerePreLoaded(IEnumerable<string> documentPaths)
        {
            foreach (string documentPath in documentPaths)
            {
                _retrievalService
                    .Received(1)
                    .GetFundingFeedDocument(documentPath, true);
            }
        }

        private void ThenNoFeedItemsWerePreLoaded()
        {
            _searchService
                .Received(0)
                .GetFeedsV3(Arg.Any<int?>(),
                    Arg.Any<int>(),
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<IEnumerable<string>>())
                .GetAwaiter()
                .GetResult();

            _retrievalService
                .Received(0)
                .GetFundingFeedDocument(Arg.Any<string>(), true)
                .GetAwaiter()
                .GetResult();
        }

        private async Task WhenThePreloadIsRun()
        {
            await _preLoader.BeginFeedItemPreLoading();
        }

        private void GivenTheSettings(int pageSize, int preLoadCount, bool shouldPreLoad, bool fileSystemCacheEnabled = true)
        {
            _settings.PageSize = pageSize;
            _settings.PreLoadCount = preLoadCount;
            _settings.ShouldPreLoad = shouldPreLoad;
            _apiFileSystemCacheSettings.IsEnabled = fileSystemCacheEnabled;
        }

        private void AndTheSearchFeedPage(int page, int pageSize, SearchFeedV3<PublishedFundingIndex> result)
        {
            _searchService.GetFeedsV3(page,
                    pageSize,
                    null, null, null)
                .Returns(result);
        }

        private SearchFeedV3<PublishedFundingIndex> NewV3SearchFeed(Action<SearchFeedV3Builder<PublishedFundingIndex>> setUp = null)
        {
            SearchFeedV3Builder<PublishedFundingIndex> builder = new SearchFeedV3Builder<PublishedFundingIndex>();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private PublishedFundingIndex NewPublishedFundingIndex(Action<PublishedFundingIndexBuilder> setUp = null)
        {
            PublishedFundingIndexBuilder builder = new PublishedFundingIndexBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}