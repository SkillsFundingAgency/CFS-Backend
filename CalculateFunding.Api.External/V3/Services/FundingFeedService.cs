using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
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
            response.ContentType = "text/plain";

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
            using (var responseStreamWriter = new StreamWriter(response.Body, leaveOpen: true) { AutoFlush = true })
            {
                const string fundingEndpointName = "notifications";
                string baseRequestPath = request.Path.Value.Substring(0, request.Path.Value.IndexOf(fundingEndpointName, StringComparison.Ordinal) + fundingEndpointName.Length);
                string fundingTrimmedRequestPath = baseRequestPath.Replace(fundingEndpointName, string.Empty).TrimEnd('/');

                string queryString = request.QueryString.Value;

                string fundingUrl = $"{request.Scheme}://{request.Host.Value}{baseRequestPath}{{0}}{(!string.IsNullOrWhiteSpace(queryString) ? queryString : "")}";

                await CreateAtomFeedHeader(searchFeed, fundingUrl, responseStreamWriter);

                List<PublishedFundingIndex> publishedFundingIndexes = searchFeed.Entries.ToList();
                int remainingCount = publishedFundingIndexes.Count;
                int batchCount = _externalEngineOptions.BlobLookupConcurrencyCount;
                int skipCount = 0;

                while (remainingCount > 0)
                {
                    ConcurrentDictionary<PublishedFundingIndex, string> fundingFeedDocuments = await GetFundingFeedDocuments(publishedFundingIndexes.Skip(skipCount).Take(batchCount));

                    remainingCount -= fundingFeedDocuments.Count;
                    skipCount += fundingFeedDocuments.Count;
                    bool isLastBatch = remainingCount <= 0;

                    await AddAtomEntryAsync(request, responseStreamWriter, fundingTrimmedRequestPath, fundingFeedDocuments, isLastBatch);
                }

                await CreateAtomFeedFooter(responseStreamWriter);
            }
        }

        private async Task CreateAtomFeedFooter(StreamWriter responseStreamWriter)
        {
            await responseStreamWriter.WriteLineAsync("]}");
        }

        private async Task<ConcurrentDictionary<PublishedFundingIndex, string>> GetFundingFeedDocuments(IEnumerable<PublishedFundingIndex> fundingIndexes)
        {
            ConcurrentDictionary<PublishedFundingIndex, string> feedContentResults = new ConcurrentDictionary<PublishedFundingIndex, string>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _externalEngineOptions.BlobLookupConcurrencyCount);
            foreach (PublishedFundingIndex feedIndex in fundingIndexes)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            //TODO; sort out the full document url as just the blob name is no good
                            string contents = await _publishedFundingRetrievalService.GetFundingFeedDocument(feedIndex.DocumentPath);
                            feedContentResults.TryAdd(feedIndex, contents);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return feedContentResults;
        }

        private async Task CreateAtomFeedHeader(SearchFeedV3<PublishedFundingIndex> searchFeed, string fundingUrl, StreamWriter responseStreamWriter)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{{");
            stringBuilder.AppendLine($"    \"id\":\"{Guid.NewGuid().ToString("N")}\",");
            stringBuilder.AppendLine($"    \"title\":\"Calculate Funding Service Funding Feed\",");
            stringBuilder.AppendLine($"    \"author\":{{");
            stringBuilder.AppendLine($"                 \"name\":\"Calculate Funding Service\",");
            stringBuilder.AppendLine($"                 \"email\":\"calculate-funding@education.gov.uk\"");
            stringBuilder.AppendLine($"               }},");
            stringBuilder.AppendLine($"    \"updated\":\"{DateTimeOffset.Now}\",");
            stringBuilder.AppendLine($"    \"rights\":\"calculate-funding@education.gov.uk\",");
            stringBuilder.AppendLine($"    \"link\": [");

            IList<CalculateFunding.Models.External.AtomItems.AtomLink> atomLinks = searchFeed.GenerateAtomLinksForResultGivenBaseUrl(fundingUrl).ToList();
            int linkCount = 0;
            foreach (CalculateFunding.Models.External.AtomItems.AtomLink link in atomLinks)
            {
                linkCount++;
                stringBuilder.AppendLine($"    {{");
                stringBuilder.AppendLine($"        \"href\":\"{link.Href}\",");
                stringBuilder.AppendLine($"        \"rel\":\"{link.Rel}\"");
                stringBuilder.AppendLine($"    }}");
                if (linkCount != atomLinks.Count)
                {
                    stringBuilder.Append(",");
                };
            }

            stringBuilder.AppendLine($"             ],");
            stringBuilder.AppendLine($"    \"atomEntry\": [");

            await responseStreamWriter.WriteLineAsync(stringBuilder.ToString());
        }

        private async Task AddAtomEntryAsync(HttpRequest request,
            StreamWriter responseStreamWriter,
            string fundingTrimmedRequestPath,
            ConcurrentDictionary<PublishedFundingIndex, string> fundingFeedDocuments,
            bool isLastBatch)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int count = 0;
            foreach (KeyValuePair<PublishedFundingIndex, string> item in fundingFeedDocuments)
            {
                count++;
                PublishedFundingIndex feedIndex = item.Key;

                string link = $"{request.Scheme}://{request.Host.Value}{fundingTrimmedRequestPath}/byId/{feedIndex.Id}";

                stringBuilder.AppendLine($"        {{");
                stringBuilder.AppendLine($"             \"id\":\"{link}\",");
                stringBuilder.AppendLine($"             \"title\":\"{feedIndex.Id}\",");
                stringBuilder.AppendLine($"             \"summary\":\"{feedIndex.Id}\",");
                stringBuilder.AppendLine($"             \"published\":\"{feedIndex.StatusChangedDate}\",");
                stringBuilder.AppendLine($"             \"updated\":\"{feedIndex.StatusChangedDate.Value}\",");
                stringBuilder.AppendLine($"             \"version\":\"{feedIndex.Version}\",");
                stringBuilder.AppendLine($"             \"link\":");
                stringBuilder.AppendLine($"                     {{");
                stringBuilder.AppendLine($"                         \"href\":\"{link}\",");
                stringBuilder.AppendLine($"                         \"rel\":\"Funding\"");
                stringBuilder.AppendLine($"                     }},");
                if (string.IsNullOrWhiteSpace(item.Value))
                {
                    stringBuilder.AppendLine($"             \"content\":null");
                }
                else
                {
                    stringBuilder.AppendLine($"             \"content\":{item.Value}");
                }
                stringBuilder.AppendLine($"        }}");

                if (!isLastBatch || count != fundingFeedDocuments.Count)
                {
                    stringBuilder.Append($",");
                }
            }

            await responseStreamWriter.WriteLineAsync(stringBuilder.ToString());
        }

        private bool IsIncompleteArchivePage(SearchFeedV3<PublishedFundingIndex> searchFeed, int? pageRef)
        {
            return pageRef != null && searchFeed.Last == pageRef && searchFeed.Entries.Count() != searchFeed.Top;
        }
    }
}
