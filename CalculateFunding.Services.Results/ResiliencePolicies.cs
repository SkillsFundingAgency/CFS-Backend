using CalculateFunding.Services.Results.Interfaces;
using Polly;

namespace CalculateFunding.Services.Results
{
    public class ResiliencePolicies : IResultsResilliencePolicies
    {
        public Policy CalculationProviderResultsSearchRepository { get; set; }

        public Policy ResultsRepository { get; set; }

        public Policy ResultsSearchRepository { get; set; }

        public Policy SpecificationsRepository { get; set; }

        public Policy AllocationNotificationFeedSearchRepository { get; set; }

        public Policy ProviderProfilingRepository { get; set; }
    }
}
