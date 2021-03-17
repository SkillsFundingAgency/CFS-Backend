using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FundingFeedService : IFundingFeedService
    {
        public const int MaxRecords = 500;

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

        /// <summary>
        /// Generate funding feed page
        /// Page behaviour should be as https://tools.ietf.org/html/rfc5005, Section 4. Archived Feeds
        /// </summary>
        /// <param name="request">Http Request</param>
        /// <param name="response">Http Response</param>
        /// <param name="pageRef">Page of historical results, null for latest items</param>
        /// <param name="fundingStreamIds">Optional funding stream IDs to filter on</param>
        /// <param name="fundingPeriodIds">Optional funding stream period IDs to filter on</param>
        /// <param name="groupingReasons">Optional grouping reasons to filter on</param>
        /// <param name="variationReasons">Optional variation reason to filter on</param>
        /// <param name="pageSize">Page size</param>
        /// <returns></returns>
        public async Task<IActionResult> GetFunding(HttpRequest request,
            HttpResponse response,
            int? pageRef,
            IEnumerable<string> fundingStreamIds = null,
            IEnumerable<string> fundingPeriodIds = null,
            IEnumerable<Models.GroupingReason> groupingReasons = null,
            IEnumerable<Models.VariationReason> variationReasons = null,
            int? pageSize = MaxRecords)
        {

            pageSize ??= MaxRecords;

            if (pageRef < 1) return new BadRequestObjectResult("Page ref should be at least 1");

            if (pageSize < 1 || pageSize > MaxRecords) return new BadRequestObjectResult($"Page size should be more that zero and less than or equal to {MaxRecords}");

            response.StatusCode = 200;
            response.ContentType = "application/json";

            SearchFeedV3<PublishedFundingIndex> searchFeed = await _feedService.GetFeedsV3(
                pageRef, pageSize.Value, fundingStreamIds, fundingPeriodIds,
                groupingReasons?.Select(x => x.ToString()),
                variationReasons?.Select(x => x.ToString()));

            if (searchFeed == null || searchFeed.TotalCount == 0 || searchFeed.Entries.IsNullOrEmpty() || IsIncompleteArchivePage(searchFeed, pageRef))
            {
                return new NotFoundResult();
            }

            await CreateAtomFeed(searchFeed, request, response);

            return new EmptyResult();
        }

        private async Task CreateAtomFeed(SearchFeedV3<PublishedFundingIndex> searchFeed, HttpRequest request, HttpResponse response)
        {
            // Using AutoFlush while writing the response. LeaveOpen required for unit testing
            await using StreamWriter responseStreamWriter = new StreamWriter(response.Body, leaveOpen: true) { AutoFlush = true };

            const string fundingEndpointName = "notifications";
            string baseRequestPath = request.Path.Value.Substring(0, request.Path.Value.IndexOf(fundingEndpointName, StringComparison.Ordinal) + fundingEndpointName.Length);
            string fundingTrimmedRequestPath = baseRequestPath.Replace(fundingEndpointName, string.Empty).TrimEnd('/');

            string queryString = request.QueryString.Value;

            string fundingUrl = $"{request.Scheme}://{request.Host.Value}{baseRequestPath}{{0}}{(!string.IsNullOrWhiteSpace(queryString) ? queryString : "")}";

            await CreateAtomFeedHeader(searchFeed, fundingUrl, responseStreamWriter);

            List<(PublishedFundingIndex Index, int Order)> publishedFundingIndexes = searchFeed.Entries.Select((_, index) => (_, index)).ToList();
            int remainingCount = publishedFundingIndexes.Count;
            int batchCount = _externalEngineOptions.BlobLookupConcurrencyCount;
            int skipCount = 0;

            while (remainingCount > 0)
            {
                IDictionary<PublishedFundingIndex, string> fundingFeedDocuments = await GetFundingFeedDocuments(publishedFundingIndexes.Skip(skipCount).Take(batchCount));

                remainingCount -= fundingFeedDocuments.Count;
                skipCount += fundingFeedDocuments.Count;
                bool isLastBatch = remainingCount <= 0;

                await AddAtomEntryAsync(request, responseStreamWriter, fundingTrimmedRequestPath, fundingFeedDocuments, isLastBatch);
            }

            await CreateAtomFeedFooter(responseStreamWriter);
        }

        private async Task CreateAtomFeedFooter(StreamWriter responseStreamWriter)
        {
            await responseStreamWriter.WriteLineAsync("]}");
        }

        private async Task<IDictionary<PublishedFundingIndex, string>> GetFundingFeedDocuments(IEnumerable<(PublishedFundingIndex Index, int Order)> fundingIndexes)
        {
            ConcurrentDictionary<PublishedFundingIndex, (string Contents, int Order)> feedContentResults = new ConcurrentDictionary<PublishedFundingIndex, (string Contents, int Order)>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _externalEngineOptions.BlobLookupConcurrencyCount);
            foreach ((PublishedFundingIndex Index, int Order) in fundingIndexes)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            //TODO; sort out the full document url as just the blob name is no good
                            string contents = await _publishedFundingRetrievalService.GetFundingFeedDocument(Index.DocumentPath);
                            feedContentResults.TryAdd(Index, (contents, Order));
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return feedContentResults.OrderBy(_ => _.Value.Order).ToDictionary(_ => _.Key, _ => _.Value.Contents);
        }

        private async Task CreateAtomFeedHeader(SearchFeedV3<PublishedFundingIndex> searchFeed, string fundingUrl, StreamWriter responseStreamWriter)
        {
            CalculateFunding.Models.External.AtomItems.AtomLink[] atomLinks = searchFeed.GenerateAtomLinksForResultGivenBaseUrl(fundingUrl).ToArray();

            await responseStreamWriter.WriteLineAsync("{");
            await responseStreamWriter.WriteLineAsync($"    \"id\":\"{Guid.NewGuid():N}\",");
            await responseStreamWriter.WriteLineAsync("    \"title\":\"Calculate Funding Service Funding Feed\",");
            await responseStreamWriter.WriteLineAsync("    \"author\":{");
            await responseStreamWriter.WriteLineAsync("                 \"name\":\"Calculate Funding Service\",");
            await responseStreamWriter.WriteLineAsync("                 \"email\":\"calculate-funding@education.gov.uk\"");
            await responseStreamWriter.WriteLineAsync("               },");
            await responseStreamWriter.WriteLineAsync($"    \"updated\":{System.Text.Json.JsonSerializer.Serialize(DateTimeOffset.Now)},");
            await responseStreamWriter.WriteLineAsync("    \"rights\":\"calculate-funding@education.gov.uk\",");
            await responseStreamWriter.WriteLineAsync("    \"link\": [");

            int linkCount = 0;

            foreach (CalculateFunding.Models.External.AtomItems.AtomLink link in atomLinks)
            {
                linkCount++;
                await responseStreamWriter.WriteLineAsync("    {");
                await responseStreamWriter.WriteLineAsync($"        \"href\":\"{link.Href}\",");
                await responseStreamWriter.WriteLineAsync($"        \"rel\":\"{link.Rel}\"");
                await responseStreamWriter.WriteLineAsync("    }");

                if (linkCount != atomLinks.Length)
                {
                    await responseStreamWriter.WriteLineAsync(",");
                };
            }

            await responseStreamWriter.WriteLineAsync("             ],");
            await responseStreamWriter.WriteLineAsync("    \"atomEntry\": [");
        }

        private async Task AddAtomEntryAsync(HttpRequest request,
            StreamWriter responseStreamWriter,
            string fundingTrimmedRequestPath,
            IDictionary<PublishedFundingIndex, string> fundingFeedDocuments,
            bool isLastBatch)
        {
            int feedDocumentCount = fundingFeedDocuments.Count;

            int count = 0;

            foreach (KeyValuePair<PublishedFundingIndex, string> item in fundingFeedDocuments)
            {
                count++;
                PublishedFundingIndex feedIndex = item.Key;

                string link = $"{request.Scheme}://{request.Host.Value}{fundingTrimmedRequestPath}/byId/{feedIndex.Id}";

                await responseStreamWriter.WriteLineAsync("        {");
                await responseStreamWriter.WriteLineAsync($"             \"id\":\"{link}\",");
                await responseStreamWriter.WriteLineAsync($"             \"title\":\"{feedIndex.Id}\",");
                await responseStreamWriter.WriteLineAsync($"             \"summary\":\"{feedIndex.Id}\",");
                await responseStreamWriter.WriteLineAsync($"             \"published\": {System.Text.Json.JsonSerializer.Serialize(feedIndex.StatusChangedDate)},");
                await responseStreamWriter.WriteLineAsync($"             \"updated\":{System.Text.Json.JsonSerializer.Serialize(feedIndex.StatusChangedDate.GetValueOrDefault())},");
                await responseStreamWriter.WriteLineAsync($"             \"version\":\"{feedIndex.Version}\",");
                await responseStreamWriter.WriteLineAsync("             \"link\":");
                await responseStreamWriter.WriteLineAsync("                     {");
                await responseStreamWriter.WriteLineAsync($"                         \"href\":\"{link}\",");
                await responseStreamWriter.WriteLineAsync("                         \"rel\":\"Funding\"");
                await responseStreamWriter.WriteLineAsync("                     },");
                if (string.IsNullOrWhiteSpace(item.Value))
                {
                    await responseStreamWriter.WriteLineAsync("             \"content\":null");
                }
                else
                {
                    await responseStreamWriter.WriteLineAsync($"             \"content\":{item.Value}");
                }
                await responseStreamWriter.WriteLineAsync("        }");

                if (!isLastBatch || count != feedDocumentCount)
                {
                    await responseStreamWriter.WriteLineAsync(",");
                }

                await responseStreamWriter.FlushAsync();
            }
        }

        private bool IsIncompleteArchivePage(SearchFeedV3<PublishedFundingIndex> searchFeed, int? pageRef)
        {
            return pageRef != null && searchFeed.Last == pageRef && searchFeed.Entries.Count() != searchFeed.Top;
        }
    }
}
