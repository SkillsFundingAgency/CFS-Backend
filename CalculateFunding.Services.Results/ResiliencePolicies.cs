using CalculateFunding.Services.Results.Interfaces;
using Polly;

namespace CalculateFunding.Services.Results
{
    public class ResiliencePolicies : IResultsResiliencePolicies
    {
        public Policy CalculationProviderResultsSearchRepository { get; set; }

        public Policy ResultsRepository { get; set; }

        public Policy ResultsSearchRepository { get; set; }

        public Policy SpecificationsApiClient { get; set; }

        public Policy ProviderProfilingRepository { get; set; }

        public Policy PublishedProviderCalculationResultsRepository { get; set; }

        public Policy PublishedProviderResultsRepository { get; set; }

        public Policy CalculationsRepository { get; set; }

        public Policy JobsApiClient { get; set; }

        public Policy ProviderCalculationResultsSearchRepository { get; set; }

        public Policy ProviderChangesRepository { get; set; }

        public Policy ProviderVersionsSearchRepository { get; set; }

        public Policy PoliciesApiClient { get; set; }
    }
}
