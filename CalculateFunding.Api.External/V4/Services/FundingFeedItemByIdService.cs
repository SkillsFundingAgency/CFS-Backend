using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
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
        private readonly IChannelUrlToIdResolver _channelUrlToIdResolver;
        private readonly Polly.AsyncPolicy _repoPolicy;
        private readonly ILogger _logger;

        public FundingFeedItemByIdService(IPublishedFundingRetrievalService publishedFundingRetrievalService,
            IReleaseManagementRepository releaseManagementRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            IChannelUrlToIdResolver channelUrlToIdResolver,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingRetrievalService, nameof(publishedFundingRetrievalService));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(channelUrlToIdResolver, nameof(channelUrlToIdResolver));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingFeedSearchRepository, nameof(resiliencePolicies.FundingFeedSearchRepository));

            _publishedFundingRetrievalService = publishedFundingRetrievalService;
            _repo = releaseManagementRepository;
            _channelUrlToIdResolver = channelUrlToIdResolver;
            _repoPolicy = resiliencePolicies.ReleaseManagementRepository;
            _logger = logger;
        }

        public async Task<IActionResult> GetFundingByFundingResultId(string channelKey, string fundingId)
        {
            Guard.IsNullOrWhiteSpace(channelKey, nameof(channelKey));
            Guard.IsNullOrWhiteSpace(fundingId, nameof(fundingId));

            int? channelId = await _channelUrlToIdResolver.ResolveUrlToChannelId(channelKey);
            if (!channelId.HasValue)
            {
                return new PreconditionFailedResult("Channel not found");
            }

            bool fundingIdExists = await _repoPolicy.ExecuteAsync(() => _repo.ContainsFundingId(channelId, fundingId));
            if (!fundingIdExists)
            {
                return new NotFoundObjectResult("Funding ID not found");
            }

            Stream fundingDocument = await _publishedFundingRetrievalService.GetFundingFeedDocument(fundingId, channelId.Value);

            if (fundingDocument == null || fundingDocument.Length == 0)
            {
                _logger.Error("Failed to find blob with id {fundingId} and channel ID: {channelId}", fundingId, channelId);
                return new NotFoundResult();
            }

            return new FileStreamResult(fundingDocument, "application/json");
        }
    }
}
