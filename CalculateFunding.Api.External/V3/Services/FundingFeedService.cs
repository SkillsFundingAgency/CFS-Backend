using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.V3.AtomItems;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FundingFeedService : IFundingFeedService
    {
        const int MaxRecords = 500;

        private readonly IFundingFeedSearchService _feedService;
        private readonly IPublishedFundingRetrievalService _publishedFundingRetrievalService;
        private readonly IExternalEngineOptions _externalEngineOptions;

        public FundingFeedService(
            IFundingFeedSearchService feedService, 
            IPublishedFundingRetrievalService publishedFundingRetrievalService,
            IExternalEngineOptions externalEngineOptions)
        {
            Guard.ArgumentNotNull(feedService, nameof(feedService));
            Guard.ArgumentNotNull(publishedFundingRetrievalService, nameof(publishedFundingRetrievalService));
            Guard.ArgumentNotNull(externalEngineOptions, nameof(externalEngineOptions));

            _feedService = feedService;
            _publishedFundingRetrievalService = publishedFundingRetrievalService;
            _externalEngineOptions = externalEngineOptions;
        }

        public async Task<IActionResult> GetFunding(HttpRequest request,
            int? pageRef,
            IEnumerable<string> fundingStreamIds = null,
            IEnumerable<string> fundingPeriodIds = null,
            IEnumerable<Models.GroupingReason> groupingReasons = null,
            IEnumerable<Models.VariationReason> variationReasons = null,
            int? pageSize = MaxRecords)
        {
            pageSize = pageSize ?? MaxRecords;

            if (pageRef < 1) return new BadRequestObjectResult("Page ref should be at least 1");

            if (pageSize < 1 || pageSize > 500) return new BadRequestObjectResult($"Page size should be more that zero and less than or equal to {MaxRecords}");

            SearchFeedV3<PublishedFundingIndex> searchFeed = await _feedService.GetFeedsV3(
                pageRef, pageSize.Value, fundingStreamIds, fundingPeriodIds, 
                groupingReasons?.Select(x => x.ToString()),
                variationReasons?.Select(x => x.ToString()));

            if (searchFeed == null || searchFeed.TotalCount == 0 || searchFeed.Entries.IsNullOrEmpty()) return new NotFoundResult();

            AtomFeed<AtomEntry> atomFeed = await CreateAtomFeed(searchFeed, request);

            return new OkObjectResult(atomFeed);
        }

        private async Task<AtomFeed<AtomEntry>> CreateAtomFeed(SearchFeedV3<PublishedFundingIndex> searchFeed, HttpRequest request)
        {
            const string fundingEndpointName = "notifications";
            string baseRequestPath = request.Path.Value.Substring(0, request.Path.Value.IndexOf(fundingEndpointName, StringComparison.Ordinal) + fundingEndpointName.Length);
            string fundingTrimmedRequestPath = baseRequestPath.Replace(fundingEndpointName, string.Empty).TrimEnd('/');

            string queryString = request.QueryString.Value;

            string fundingUrl = $"{request.Scheme}://{request.Host.Value}{baseRequestPath}{{0}}{(!string.IsNullOrWhiteSpace(queryString) ? queryString : "")}";

            AtomFeed<AtomEntry> atomFeed = CreateAtomFeedEntry(searchFeed, fundingUrl);

            ConcurrentDictionary<string, object> feedContentResults = new ConcurrentDictionary<string, object>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _externalEngineOptions.BlobLookupConcurrencyCount);
            foreach (PublishedFundingIndex feedIndex in searchFeed.Entries)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            //TODO; sort out the full document url as just the blob name is no good

                            string contents = await _publishedFundingRetrievalService.GetFundingFeedDocument(feedIndex.DocumentPath);

                            // Need to convert to an object, so JSON.NET can reserialise the contents, otherwise the string is escaped.
                            // Future TODO: change whole feed to output via text, instead of objects
                            object contentsObject = JsonConvert.DeserializeObject(contents);

                            feedContentResults.TryAdd(feedIndex.Id, contentsObject);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            foreach (PublishedFundingIndex feedIndex in searchFeed.Entries)
            {
                AddAtomEntry(request, fundingTrimmedRequestPath, feedIndex, feedContentResults, atomFeed);
            }

            return atomFeed;
        }

        private AtomFeed<AtomEntry> CreateAtomFeedEntry(SearchFeedV3<PublishedFundingIndex> searchFeed, string fundingUrl)
        {
            AtomFeed<AtomEntry> atomFeed = new AtomFeed<AtomEntry>
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = "Calculate Funding Service Funding Feed",
                Author = new CalculateFunding.Models.External.AtomItems.AtomAuthor
                {
                    Name = "Calculate Funding Service",
                    Email = "calculate-funding@education.gov.uk"
                },
                Updated = DateTimeOffset.Now,
                Rights = "Copyright (C) 2019 Department for Education",
                Link = searchFeed.GenerateAtomLinksForResultGivenBaseUrl(fundingUrl).ToList(),
                AtomEntry = new List<AtomEntry>(),
                IsArchived = searchFeed.IsArchivePage
            };
            return atomFeed;
        }

        private void AddAtomEntry(HttpRequest request,
            string fundingTrimmedRequestPath,
            PublishedFundingIndex feedIndex,
            ConcurrentDictionary<string, object> feedContentResults,
            AtomFeed<AtomEntry> atomFeed)
        {
            string link = $"{request.Scheme}://{request.Host.Value}{fundingTrimmedRequestPath}/byId/{feedIndex.Id}";

            feedContentResults.TryGetValue(feedIndex.Id, out object contentsObject);

            atomFeed.AtomEntry.Add(new AtomEntry
            {
                Id = link,
                Title = feedIndex.Id,
                Summary = feedIndex.Id,
                Published = feedIndex.StatusChangedDate,
                Updated = feedIndex.StatusChangedDate.Value,
                Version = feedIndex.Version,
                Link = new CalculateFunding.Models.External.AtomItems.AtomLink(link, "Funding"),
                Content = contentsObject,
            });
        }


    }
}
