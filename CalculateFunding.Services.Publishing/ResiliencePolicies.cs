using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class ResiliencePolicies : IPublishingResiliencePolicies, IJobHelperResiliencePolicies
    {
        public Policy CalculationResultsRepository { get; set; }

        public Policy SpecificationsRepositoryPolicy { get; set; }

        public Policy JobsApiClient { get; set; }

        public Policy ProvidersApiClient { get; set; }

        public Policy PublishedFundingRepository { get; set; }

        public Policy PublishedProviderVersionRepository { get; set; }

        public Policy FundingFeedSearchRepository { get; set; }

        public Policy PublishedFundingBlobRepository { get; set; }

        public Policy BlobClient { get; set; }

        public Policy CalculationsApiClient { get; set; }
        public Policy PoliciesApiClient { get; set; }

        public Policy PublishedProviderSearchRepository { get; set; }
    }
}
