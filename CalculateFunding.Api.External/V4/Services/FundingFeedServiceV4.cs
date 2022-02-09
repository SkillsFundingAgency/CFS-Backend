using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public class FundingFeedService : IFundingFeedService
    {
        public const int MaxRecords = 500;

        private readonly IReleaseManagementRepository _repo;
        private readonly IPublishedFundingRetrievalService _publishedFundingRetrievalService;
        private readonly IChannelUrlToChannelResolver _channelUrlToChannelResolver;
        private readonly IExternalApiFeedWriter _feedWriter;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _releaseManagementPolicy;

        public FundingFeedService(
            IReleaseManagementRepository releaseManagementRepository,
            IPublishedFundingRetrievalService publishedFundingRetrievalService,
            IChannelUrlToChannelResolver channelUrlToChannelResolver,
            IExternalApiFeedWriter externalApiFeedWriter,
            ILogger logger,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(publishedFundingRetrievalService, nameof(publishedFundingRetrievalService));
            Guard.ArgumentNotNull(externalApiFeedWriter, nameof(externalApiFeedWriter));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(channelUrlToChannelResolver, nameof(channelUrlToChannelResolver));

            _repo = releaseManagementRepository;
            _publishedFundingRetrievalService = publishedFundingRetrievalService;
            _channelUrlToChannelResolver = channelUrlToChannelResolver;
            _feedWriter = externalApiFeedWriter;
            _logger = logger;

            _releaseManagementPolicy = publishingResiliencePolicies.ReleaseManagementRepository;
        }

        /// <summary>
        /// Generate funding feed page
        /// Page behaviour should be as https://tools.ietf.org/html/rfc5005, Section 4. Archived Feeds
        /// </summary>
        /// <param name="request">Http Request</param>
        /// <param name="response">Http Response</param>
        /// <param name="pageRef">Page of historical results, null for latest items</param>
        /// <param name="channelUrlKey">Channel key (friendly name)</param>
        /// <param name="fundingStreamIds">Optional funding stream IDs to filter on</param>
        /// <param name="fundingPeriodIds">Optional funding stream period IDs to filter on</param>
        /// <param name="groupingReasons">Optional grouping reasons to filter on</param>
        /// <param name="variationReasons">Optional variation reason to filter on</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public async Task<ActionResult<SearchFeedResult<ExternalFeedFundingGroupItem>>> GetFundingNotificationFeedPage(HttpRequest request,
            HttpResponse response,
            int? pageRef,
            string channelUrlKey,
            IEnumerable<string> fundingStreamIds = null,
            IEnumerable<string> fundingPeriodIds = null,
            IEnumerable<Models.GroupingReason> groupingReasons = null,
            IEnumerable<Models.VariationReason> variationReasons = null,
            int? pageSize = MaxRecords,
           CancellationToken cancellationToken = default(CancellationToken))
        {
            Channel channel = await _channelUrlToChannelResolver.ResolveUrlToChannel(channelUrlKey);

            if (channel == null)
            {
                return new PreconditionFailedResult("Channel does not exist");
            }

            pageSize ??= MaxRecords;

            if (pageRef < 1) return new BadRequestObjectResult("Page ref should be at least 1");

            if (pageSize < 1 || pageSize > MaxRecords) return new BadRequestObjectResult($"Page size should be more that zero and less than or equal to {MaxRecords}");

            Stopwatch sw = Stopwatch.StartNew();

            SearchFeedResult<ExternalFeedFundingGroupItem> searchFeed = await GetSearchFeedResultForPage(
                pageRef, pageSize.Value, channel.ChannelId, fundingStreamIds, fundingPeriodIds,
                groupingReasons?.Select(x => x.ToString()),
                variationReasons?.Select(x => x.ToString()));

            sw.Stop();

            _logger.Debug("Feed query executed in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            if (searchFeed == null || searchFeed.TotalCount == 0 || searchFeed.Entries.IsNullOrEmpty() || IsIncompleteArchivePage(searchFeed, pageRef))
            {
                return new NotFoundResult();
            }

            response.StatusCode = 200;
            response.ContentType = "application/json";

            try
            {
                await CreateAtomFeed(searchFeed, request, response, channel.ChannelCode, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return new InternalServerErrorResult(ex.Message);
            }

            return new EmptyResult();
        }

        private async Task CreateAtomFeed(SearchFeedResult<ExternalFeedFundingGroupItem> searchFeed,
                                          HttpRequest request,
                                          HttpResponse response,
                                          string channelCode,
                                          CancellationToken cancellationToken)
        {
            const string fundingEndpointName = "notifications";
            string baseRequestPath = request.Path.Value.Substring(0, request.Path.Value.IndexOf(fundingEndpointName, StringComparison.Ordinal) + fundingEndpointName.Length);
            string fundingTrimmedRequestPath = baseRequestPath.Replace(fundingEndpointName, string.Empty).TrimEnd('/');

            string queryString = request.QueryString.Value;

            string fundingUrl = $"{request.Scheme}://{request.Host.Value}{baseRequestPath}{{0}}{(!string.IsNullOrWhiteSpace(queryString) ? queryString : "")}";

            await _feedWriter.OutputFeedHeader(searchFeed, fundingUrl, response.BodyWriter);

            const int batchSize = 50;

            List<IEnumerable<ExternalFeedFundingGroupItem>> contentOutputBatch = new List<IEnumerable<ExternalFeedFundingGroupItem>>(searchFeed.Entries.ToBatches(batchSize));

            for (int i = 0; i < contentOutputBatch.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                IEnumerable<ExternalFeedFundingGroupItem> batchItems = contentOutputBatch[i];

                Stopwatch sw = Stopwatch.StartNew();

                IDictionary<ExternalFeedFundingGroupItem, Stream> contents = await _publishedFundingRetrievalService.GetFundingFeedDocuments(batchItems, channelCode, cancellationToken);

                sw.Stop();

                _logger.Debug("Batch of document retrieved in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

                bool isLastBatch = i == contentOutputBatch.Count - 1;

                await OutputFeedItemBatch(request,
                                          response.BodyWriter,
                                          fundingTrimmedRequestPath,
                                          contents,
                                          isLastBatch,
                                          channelCode,
                                          cancellationToken);
            }

            await _feedWriter.OutputFeedFooter(response.BodyWriter);
            await response.BodyWriter.FlushAsync();
        }


        private async Task OutputFeedItemBatch(HttpRequest request,
            PipeWriter writer,
            string fundingTrimmedRequestPath,
            IEnumerable<KeyValuePair<ExternalFeedFundingGroupItem, Stream>> fundingFeedDocuments,
            bool isLastBatch,
            string channelCode,
            CancellationToken cancellationToken)
        {
            int feedDocumentCount = fundingFeedDocuments.Count();

            int count = 0;

            List<KeyValuePair<ExternalFeedFundingGroupItem, Stream>> noContentDocuments = fundingFeedDocuments.Where(x => x.Value is null || x.Value.Length == 0).ToList();

            if (noContentDocuments.Any())
            {
                string message = $"No funding content blob found for funding ID in channel {channelCode}: {string.Join(',', noContentDocuments.Select(x => x.Key.FundingId))}.";
                throw new Exception(message);
            }

            foreach (KeyValuePair<ExternalFeedFundingGroupItem, Stream> item in fundingFeedDocuments)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                count++;
                ExternalFeedFundingGroupItem feedItem = item.Key;

                string link = $"{request.Scheme}://{request.Host.Value}{fundingTrimmedRequestPath}/byId/{feedItem.FundingId}";

                bool hasMoreItems = !isLastBatch || (isLastBatch && count != feedDocumentCount);

                await _feedWriter.OutputFeedItem(writer, link, item.Key, item.Value, hasMoreItems);

                await writer.FlushAsync();
            }
        }

        private bool IsIncompleteArchivePage(SearchFeedResult<ExternalFeedFundingGroupItem> searchFeed, int? pageRef)
        {
            return pageRef != null && searchFeed.Last == pageRef && searchFeed.Entries.Count() != searchFeed.Top;
        }

        public async Task<SearchFeedResult<ExternalFeedFundingGroupItem>> GetSearchFeedResultForPage(int? pageRef,
           int top,
           int channelId,
           IEnumerable<string> fundingStreamIds = null,
           IEnumerable<string> fundingPeriodIds = null,
           IEnumerable<string> groupingReasons = null,
           IEnumerable<string> variationReasons = null)
        {
            if (pageRef < 1)
            {
                throw new ArgumentException("Page ref cannot be less than one", nameof(pageRef));
            }

            if (top < 1)
            {
                top = 500;
            }

            int totalCount = await _releaseManagementPolicy.ExecuteAsync(() => _repo.QueryPublishedFundingCount(
                channelId,
                fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons));

            bool pageRefRequested = pageRef.HasValue;

            IEnumerable<ExternalFeedFundingGroupItem> results = await _releaseManagementPolicy.ExecuteAsync(() =>
                _repo.QueryPublishedFunding(
                    channelId,
                    fundingStreamIds,
                    fundingPeriodIds,
                    groupingReasons,
                    variationReasons,
                    top,
                    pageRef,
                    totalCount));

            pageRef ??= new LastPage(totalCount, top);

            IEnumerable<ExternalFeedFundingGroupItem> fundingFeedResults = pageRefRequested ? results : results.Reverse().ToArray();

            return new SearchFeedResult<ExternalFeedFundingGroupItem>
            {
                PageRef = pageRef.Value,
                Top = top,
                TotalCount = totalCount,
                Entries = fundingFeedResults
            };
        }
    }
}
