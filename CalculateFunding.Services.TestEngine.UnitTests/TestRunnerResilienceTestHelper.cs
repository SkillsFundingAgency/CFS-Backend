using CalculateFunding.Services.TestRunner.Interfaces;
using Polly;

namespace CalculateFunding.Services.TestRunner.UnitTests
{
    public static class TestRunnerResilienceTestHelper
    {
        public static ITestRunnerResiliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                CalculationsApiClient = Policy.NoOpAsync(),
                CacheProviderRepository = Policy.NoOpAsync(),
                ProviderResultsRepository = Policy.NoOpAsync(),
                ProviderSourceDatasetsRepository = Policy.NoOpAsync(),
                ScenariosRepository = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                TestResultsRepository = Policy.NoOpAsync(),
                TestResultsSearchRepository = Policy.NoOpAsync(),
            };
        }
    }
}
