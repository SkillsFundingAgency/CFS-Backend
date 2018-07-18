using Polly;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestRunnerResiliencePolicies
    {
        Policy TestResultsRepository { get; set; }

        Policy TestResultsSearchRepository { get; set; }

        Policy BuildProjectRepository { get; set; }

        Policy ProviderResultsRepository { get; set; }

        Policy CacheProviderRepository { get; set; }

        Policy SpecificationRepository { get; set; }

        Policy ScenariosRepository { get; set; }

        Policy ProviderSourceDatasetsRepository { get; set; }
    }
}
