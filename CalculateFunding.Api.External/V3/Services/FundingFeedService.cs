using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FundingFeedService : IFundingFeedService
    {
        public const int MaxRecords = 500;

        private readonly IFundingFeedSearchService _feedService;
        private readonly IPublishedFundingRetrievalService _publishedFundingRetrievalService;
        private readonly IExternalEngineOptions _externalEngineOptions;
        private readonly ILogger _logger;

        public FundingFeedService(
            IFundingFeedSearchService feedService,
            IPublishedFundingRetrievalService publishedFundingRetrievalService,
            IExternalEngineOptions externalEngineOptions,
            ILogger logger)
        {
            Guard.ArgumentNotNull(feedService, nameof(feedService));
            Guard.ArgumentNotNull(publishedFundingRetrievalService, nameof(publishedFundingRetrievalService));
            Guard.ArgumentNotNull(externalEngineOptions, nameof(externalEngineOptions));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _feedService = feedService;
            _publishedFundingRetrievalService = publishedFundingRetrievalService;
            _externalEngineOptions = externalEngineOptions;
            _logger = logger;
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

            SearchFeedResult<PublishedFundingIndex> searchFeed = await _feedService.GetFeedsV3(
                pageRef, pageSize.Value, fundingStreamIds, fundingPeriodIds,
                groupingReasons?.Select(x => x.ToString()),
                variationReasons?.Select(x => x.ToString()));

            if (searchFeed == null || searchFeed.TotalCount == 0 || searchFeed.Entries.IsNullOrEmpty() || IsIncompleteArchivePage(searchFeed, pageRef))
            {
                return new NotFoundResult();
            }

            response.StatusCode = 200;
            response.ContentType = "application/json";

            try
            {
                await CreateAtomFeed(searchFeed, request, response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return new InternalServerErrorResult(ex.Message);
            }

            return new EmptyResult();
        }

        private async Task CreateAtomFeed(SearchFeedResult<PublishedFundingIndex> searchFeed, HttpRequest request, HttpResponse response)
        {
            const string fundingEndpointName = "notifications";
            string baseRequestPath = request.Path.Value.Substring(0, request.Path.Value.IndexOf(fundingEndpointName, StringComparison.Ordinal) + fundingEndpointName.Length);
            string fundingTrimmedRequestPath = baseRequestPath.Replace(fundingEndpointName, string.Empty).TrimEnd('/');

            string queryString = request.QueryString.Value;

            string fundingUrl = $"{request.Scheme}://{request.Host.Value}{baseRequestPath}{{0}}{(!string.IsNullOrWhiteSpace(queryString) ? queryString : "")}";

            await CreateAtomFeedHeader(searchFeed, fundingUrl, response.BodyWriter);

            int batchSize = 50;

            List<IEnumerable<PublishedFundingIndex>> list = new List<IEnumerable<PublishedFundingIndex>>(searchFeed.Entries.ToBatches(batchSize));

            for (int i = 0; i < list.Count; i++)
            {
                IEnumerable<PublishedFundingIndex> batchItems = list[i];

                IDictionary<PublishedFundingIndex, string> contents = await GetFundingFeedDocuments(batchItems);

                bool isLastBatch = i == list.Count - 1;

                await AddAtomEntryAsync(request, response.BodyWriter, fundingTrimmedRequestPath, contents, isLastBatch);
            }

            await CreateAtomFeedFooter(response.BodyWriter);
            await response.BodyWriter.FlushAsync();
        }


        private async Task<IDictionary<PublishedFundingIndex, string>> GetFundingFeedDocuments(IEnumerable<PublishedFundingIndex> batchItems)
        {
            ConcurrentDictionary<PublishedFundingIndex, string> feedContentResults = new ConcurrentDictionary<PublishedFundingIndex, string>(_externalEngineOptions.BlobLookupConcurrencyCount, batchItems.Count());

            List<Task> allTasks = new List<Task>(batchItems.Count());
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _externalEngineOptions.BlobLookupConcurrencyCount);
            foreach (PublishedFundingIndex item in batchItems)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            //TODO; sort out the full document url as just the blob name is no good
                            string contents = await _publishedFundingRetrievalService.GetFundingFeedDocument(item.DocumentPath);
                            feedContentResults.TryAdd(item, contents);

                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return EnsureOrderedReturnOfItemsBasedOnInput(batchItems, feedContentResults);
        }

        private static IDictionary<PublishedFundingIndex, string> EnsureOrderedReturnOfItemsBasedOnInput(IEnumerable<PublishedFundingIndex> batchItems, ConcurrentDictionary<PublishedFundingIndex, string> feedContentResults)
        {
            Dictionary<PublishedFundingIndex, string> result = new Dictionary<PublishedFundingIndex, string>();
            foreach (PublishedFundingIndex item in batchItems)
            {
                result.Add(item, feedContentResults[item]);
            }

            return result;
        }

        private async Task CreateAtomFeedHeader(SearchFeedResult<PublishedFundingIndex> searchFeed, string fundingUrl, PipeWriter writer)
        {
            CalculateFunding.Models.External.AtomItems.AtomLink[] atomLinks = searchFeed.GenerateAtomLinksForResultGivenBaseUrl(fundingUrl).ToArray();

            await writer.WriteAsync("{");
            await writer.WriteAsync($"    \"id\":\"{Guid.NewGuid():N}\",");
            await writer.WriteAsync("    \"title\":\"Calculate Funding Service Funding Feed\",");
            await writer.WriteAsync("    \"author\":{");
            await writer.WriteAsync("                 \"name\":\"Calculate Funding Service\",");
            await writer.WriteAsync("                 \"email\":\"calculate-funding@education.gov.uk\"");
            await writer.WriteAsync("               },");
            await writer.WriteAsync($"    \"updated\":{System.Text.Json.JsonSerializer.Serialize(DateTimeOffset.Now)},");
            await writer.WriteAsync("    \"rights\":\"calculate-funding@education.gov.uk\",");
            await writer.WriteAsync("    \"link\": [");

            int linkCount = 0;

            foreach (CalculateFunding.Models.External.AtomItems.AtomLink link in atomLinks)
            {
                linkCount++;
                await writer.WriteAsync("    {");
                await writer.WriteAsync($"        \"href\":\"{link.Href}\",");
                await writer.WriteAsync($"        \"rel\":\"{link.Rel}\"");
                await writer.WriteAsync("    }");

                if (linkCount != atomLinks.Length)
                {
                    await writer.WriteAsync(",");
                };
            }

            await writer.WriteAsync("             ],");
            await writer.WriteAsync("    \"atomEntry\": [");
        }

        private async Task AddAtomEntryAsync(HttpRequest request,
            PipeWriter writer,
            string fundingTrimmedRequestPath,
            IEnumerable<KeyValuePair<PublishedFundingIndex, string>> fundingFeedDocuments,
            bool isLastBatch)
        {
            int feedDocumentCount = fundingFeedDocuments.Count();

            int count = 0;

            var noContentDocuments = fundingFeedDocuments.Where(x => string.IsNullOrWhiteSpace(x.Value)).ToList();

            if (noContentDocuments.Any())
            {
                string message = $"No funding content blob found for document path: {string.Join(',', noContentDocuments.Select(x => x.Key.DocumentPath))}.";
                throw new Exception(message);
            }

            foreach (KeyValuePair<PublishedFundingIndex, string> item in fundingFeedDocuments)
            {
                count++;
                PublishedFundingIndex feedIndex = item.Key;

                string link = $"{request.Scheme}://{request.Host.Value}{fundingTrimmedRequestPath}/byId/{feedIndex.Id}";

                await writer.WriteAsync("        {");
                await writer.WriteAsync($"             \"id\":\"{link}\",");
                await writer.WriteAsync($"             \"title\":\"{feedIndex.Id}\",");
                await writer.WriteAsync($"             \"summary\":\"{feedIndex.Id}\",");
                await writer.WriteAsync($"             \"published\": {System.Text.Json.JsonSerializer.Serialize(feedIndex.StatusChangedDate)},");
                await writer.WriteAsync($"             \"updated\":{System.Text.Json.JsonSerializer.Serialize(feedIndex.StatusChangedDate.GetValueOrDefault())},");
                await writer.WriteAsync($"             \"version\":\"{feedIndex.Version}\",");
                await writer.WriteAsync("             \"link\":");
                await writer.WriteAsync("                     {");
                await writer.WriteAsync($"                         \"href\":\"{link}\",");
                await writer.WriteAsync("                         \"rel\":\"Funding\"");
                await writer.WriteAsync("                     },");
                await writer.WriteAsync("             \"content\":");
                await writer.WriteAsync(item.Value);
                await writer.WriteAsync("        }");

                if (!isLastBatch || (isLastBatch && count != feedDocumentCount))
                {
                    await writer.WriteAsync(",");
                }

                await writer.FlushAsync();
            }
        }

        private bool IsIncompleteArchivePage(SearchFeedResult<PublishedFundingIndex> searchFeed, int? pageRef)
        {
            return pageRef != null && searchFeed.Last == pageRef && searchFeed.Entries.Count() != searchFeed.Top;
        }

        private async Task CreateAtomFeedFooter(PipeWriter writer)
        {
            await writer.WriteAsync("]}");
        }
    }
}
