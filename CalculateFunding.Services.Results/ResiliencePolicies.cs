using CalculateFunding.Services.Results.Interfaces;
using Polly;

namespace CalculateFunding.Services.Results
{
    public class ResiliencePolicies : IResultsResiliencePolicies
    {
        public AsyncPolicy CalculationProviderResultsSearchRepository { get; set; }

        public AsyncPolicy ResultsRepository { get; set; }

        public AsyncPolicy ResultsSearchRepository { get; set; }

        public AsyncPolicy SpecificationsApiClient { get; set; }

        public AsyncPolicy CalculationsApiClient { get; set; }

        public AsyncPolicy ProviderProfilingRepository { get; set; }

        public AsyncPolicy PublishedProviderCalculationResultsRepository { get; set; }

        public AsyncPolicy PublishedProviderResultsRepository { get; set; }

        public AsyncPolicy CalculationsRepository { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }

        public AsyncPolicy ProviderCalculationResultsSearchRepository { get; set; }

        public AsyncPolicy ProviderChangesRepository { get; set; }

        public AsyncPolicy ProviderVersionsSearchRepository { get; set; }

        public AsyncPolicy PoliciesApiClient { get; set; }
        
        public AsyncPolicy BlobClient { get; set; }

        public AsyncPolicy CacheProvider { get; set; }
    }
}
