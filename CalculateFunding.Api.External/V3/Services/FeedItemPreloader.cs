using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Search;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publising.Interfaces;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FeedItemPreLoader : IFeedItemPreloader
    {
        private class PageNumber
        {
            public PageNumber(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }
        
        private readonly IFeedItemPreloaderSettings _settings;
        private readonly IPublishedFundingRetrievalService _retrievalService;
        private readonly IFundingFeedSearchService _searchService;
        private readonly IFileSystemCache _cache;

        public FeedItemPreLoader(IFeedItemPreloaderSettings settings,
            IPublishedFundingRetrievalService retrievalService,
            IFundingFeedSearchService searchService, 
            IFileSystemCache cache)
        {
            Guard.ArgumentNotNull(settings, nameof(settings));
            Guard.ArgumentNotNull(retrievalService, nameof(retrievalService));
            Guard.ArgumentNotNull(searchService, nameof(searchService));
            Guard.ArgumentNotNull(cache, nameof(cache));

            _settings = settings;
            _retrievalService = retrievalService;
            _searchService = searchService;
            _cache = cache;
        }

        public void EnsureFoldersExists()
        {
            _cache.EnsureFoldersExist();
        }

        public async Task BeginFeedItemPreLoading()
        {
            if (!_settings.ShouldPreLoad) return;

            int pageCount = (int) Math.Ceiling(_settings.PreLoadCount / (float)_settings.PageSize);

            SemaphoreSlim pageThrottle = new SemaphoreSlim(4, 4);
            List<Task> pagePreLoadTasks = new List<Task>();

            for (int page = 1; page <= pageCount; page++)
            {
                await pageThrottle.WaitAsync();
                
                PageNumber pageNumber = new PageNumber(page);

                pagePreLoadTasks.Add(Task.Run(() => PreLoadPage(pageNumber, pageThrottle)));
            }

            await TaskHelper.WhenAllAndThrow(pagePreLoadTasks.ToArray());
        }

        private async Task PreLoadPage(PageNumber pageNumber, SemaphoreSlim pageThrottle)
        {
            try
            {
                SearchFeedV3<PublishedFundingIndex> resultsPage = await _searchService.GetFeedsV3(pageNumber.Value, _settings.PageSize, orderBy: "statusChangedDate desc");

                List<Task> cacheDocumentTasks = new List<Task>();
                SemaphoreSlim cacheThrottle = new SemaphoreSlim(10, 10);

                foreach (PublishedFundingIndex publishedFundingIndex in resultsPage.Entries)
                {
                    await cacheThrottle.WaitAsync();

                    cacheDocumentTasks.Add(Task.Run(() => CachePublishedFundingDocument(publishedFundingIndex, cacheThrottle)));
                }

                await TaskHelper.WhenAllAndThrow(cacheDocumentTasks.ToArray());
            }
            finally
            {
                pageThrottle.Release();
            }
        }

        private async Task CachePublishedFundingDocument(PublishedFundingIndex publishedFundingIndex, SemaphoreSlim cacheThrottle)
        {
            try
            {
                await _retrievalService.GetFundingFeedDocument(publishedFundingIndex.DocumentPath, true);
            }
            finally
            {
                cacheThrottle.Release();
            }
        }
    }
}