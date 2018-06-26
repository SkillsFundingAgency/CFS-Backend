using CalculateFunding.Services.TestRunner.Interfaces;
using Polly;

namespace CalculateFunding.Services.TestRunner
{
    public class ResiliencePolicies : ITestRunnerResiliencePolicies
    {
        public Policy TestResultsRepository { get; set; }

        public Policy TestResultsSearchRepository { get; set; }

        public Policy BuildProjectRepository { get; set; }

        public Policy ProviderResultsRepository { get; set; }

        public Policy CacheProviderRepository { get; set; }

        public Policy SpecificationRepository { get; set; }

        public Policy ScenariosRepository { get; set; }

        public Policy ProviderSourceDatasetsRepository { get; set; }
    }
}
