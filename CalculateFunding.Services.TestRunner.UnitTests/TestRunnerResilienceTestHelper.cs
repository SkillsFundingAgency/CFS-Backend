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
                BuildProjectRepository = Policy.NoOpAsync(),
                CacheProviderRepository = Policy.NoOpAsync(),
                ProviderResultsRepository = Policy.NoOpAsync(),
                ProviderSourceDatasetsRepository = Policy.NoOpAsync(),
                ScenariosRepository = Policy.NoOpAsync(),
                SpecificationRepository = Policy.NoOpAsync(),
                TestResultsRepository = Policy.NoOpAsync(),
                TestResultsSearchRepository = Policy.NoOpAsync(),
            };
        }
    }
}
