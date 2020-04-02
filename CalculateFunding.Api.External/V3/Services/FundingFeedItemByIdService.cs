using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FundingFeedItemByIdService : IFundingFeedItemByIdService
    {
        private readonly IPublishedFundingRetrievalService _publishedFundingRetrievalService;
        private readonly ISearchRepository<PublishedFundingIndex> _fundingSearchRepository;
        private readonly Polly.AsyncPolicy _fundingSearchRepositoryPolicy;
        private readonly ILogger _logger;

        public FundingFeedItemByIdService(IPublishedFundingRetrievalService publishedFundingRetrievalService,
            ISearchRepository<PublishedFundingIndex> fundingSearchRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingRetrievalService, nameof(publishedFundingRetrievalService));
            Guard.ArgumentNotNull(fundingSearchRepository, nameof(fundingSearchRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingFeedSearchRepository, nameof(resiliencePolicies.FundingFeedSearchRepository));

            _publishedFundingRetrievalService = publishedFundingRetrievalService;
            _fundingSearchRepository = fundingSearchRepository;
            _fundingSearchRepositoryPolicy = resiliencePolicies.FundingFeedSearchRepository;
            _logger = logger;
        }

        public async Task<IActionResult> GetFundingByFundingResultId(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            PublishedFundingIndex fundingIndexedDocument = await _fundingSearchRepositoryPolicy.ExecuteAsync(() => _fundingSearchRepository.SearchById(id));

            if (fundingIndexedDocument == null)
            {
                return new NotFoundResult();
            }

            string fundingDocument = await _publishedFundingRetrievalService.GetFundingFeedDocument(fundingIndexedDocument.DocumentPath);

            if (string.IsNullOrWhiteSpace(fundingDocument))
            {
                _logger.Error("Failed to find blob with id {id} and document path: {documentPath}", id, fundingIndexedDocument.DocumentPath);
                return new NotFoundResult();
            }

            return new ContentResult() { Content = fundingDocument, ContentType = "application/json", StatusCode = (int)HttpStatusCode.OK };
        }
    }
}
