using CalculateFunding.Services.TestRunner.Interfaces;
using Polly;

namespace CalculateFunding.Services.TestRunner
{
    public class ResiliencePolicies : ITestRunnerResiliencePolicies
    {
        public AsyncPolicy TestResultsRepository { get; set; }

        public AsyncPolicy TestResultsSearchRepository { get; set; }

        public AsyncPolicy CalculationsApiClient { get; set; }

        public AsyncPolicy ProviderResultsRepository { get; set; }

        public AsyncPolicy CacheProviderRepository { get; set; }

        public AsyncPolicy SpecificationsApiClient { get; set; }

        public AsyncPolicy ScenariosRepository { get; set; }

        public AsyncPolicy ProviderSourceDatasetsRepository { get; set; }
    }
}
