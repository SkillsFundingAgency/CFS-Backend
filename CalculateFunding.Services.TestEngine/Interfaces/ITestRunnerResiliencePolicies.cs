using Polly;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestRunnerResiliencePolicies
    {
        AsyncPolicy TestResultsRepository { get; set; }

        AsyncPolicy TestResultsSearchRepository { get; set; }

        AsyncPolicy CalculationsApiClient { get; set; }

        AsyncPolicy ProviderResultsRepository { get; set; }

        AsyncPolicy CacheProviderRepository { get; set; }

        AsyncPolicy SpecificationsApiClient { get; set; }

        AsyncPolicy ScenariosRepository { get; set; }

        AsyncPolicy ProviderSourceDatasetsRepository { get; set; }
    }
}
