using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public class FundingFeedItemByIdService : IFundingFeedItemByIdService
    {
        private readonly IPublishedFundingRetrievalService _publishedFundingRetrievalService;
        private readonly IReleaseManagementRepository _repo;
        private readonly IChannelUrlToChannelResolver _channelUrlToChannelResolver;
        private readonly Polly.AsyncPolicy _repoPolicy;
        private readonly ILogger _logger;

        public FundingFeedItemByIdService(IPublishedFundingRetrievalService publishedFundingRetrievalService,
            IReleaseManagementRepository releaseManagementRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            IChannelUrlToChannelResolver channelUrlToChannelResolver,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingRetrievalService, nameof(publishedFundingRetrievalService));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(channelUrlToChannelResolver, nameof(channelUrlToChannelResolver));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingFeedSearchRepository, nameof(resiliencePolicies.FundingFeedSearchRepository));

            _publishedFundingRetrievalService = publishedFundingRetrievalService;
            _repo = releaseManagementRepository;
            _channelUrlToChannelResolver = channelUrlToChannelResolver;
            _repoPolicy = resiliencePolicies.ReleaseManagementRepository;
            _logger = logger;
        }

        public async Task<IActionResult> GetFundingByFundingResultId(string channelKey, string fundingId)
        {
            Guard.IsNullOrWhiteSpace(channelKey, nameof(channelKey));
            Guard.IsNullOrWhiteSpace(fundingId, nameof(fundingId));

            Channel channel = await _channelUrlToChannelResolver.ResolveUrlToChannel(channelKey);
            if (channel == null)
            {
                return new PreconditionFailedResult("Channel not found");
            }

            bool fundingIdExists = await _repoPolicy.ExecuteAsync(() => _repo.ContainsFundingId(channel.ChannelId, fundingId));
            if (!fundingIdExists)
            {
                return new NotFoundObjectResult("Funding ID not found");
            }

            Stream fundingDocument = await _publishedFundingRetrievalService.GetFundingFeedDocument(fundingId, channel.ChannelCode);

            if (fundingDocument == null || fundingDocument.Length == 0)
            {
                _logger.Error("Failed to find blob with id {fundingId} and channel ID: {channelCode}", fundingId, channel.ChannelCode);
                return new NotFoundResult();
            }
            Stream contents = _channelUrlToChannelResolver.GetContentWithChannelVersion(fundingDocument, channel.ChannelCode).Result;
            return new FileStreamResult(contents, "application/json");
        }
    }
}
