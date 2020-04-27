using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class ResiliencePolicies : IPublishingResiliencePolicies
    {
        public AsyncPolicy CalculationResultsRepository { get; set; }

        public AsyncPolicy SpecificationsRepositoryPolicy { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }

        public AsyncPolicy ProvidersApiClient { get; set; }

        public AsyncPolicy PublishedFundingRepository { get; set; }

        public AsyncPolicy PublishedProviderVersionRepository { get; set; }

        public AsyncPolicy FundingFeedSearchRepository { get; set; }
        
        public AsyncPolicy PublishedProviderSearchRepository { get; set; }

        public AsyncPolicy PublishedFundingBlobRepository { get; set; }

        public AsyncPolicy BlobClient { get; set; }

        public AsyncPolicy CalculationsApiClient { get; set; }
        
        public AsyncPolicy PoliciesApiClient { get; set; }
        
        public AsyncPolicy SpecificationsApiClient { get; set; }

        public AsyncPolicy PublishedIndexSearchResiliencePolicy { get; set; }
        
        public AsyncPolicy FundingStreamPaymentDatesRepository { get; set; }
        
        public AsyncPolicy CacheProvider { get; set; }
    }
}
