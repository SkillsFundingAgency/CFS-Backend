using CalculateFunding.Services.Results.Interfaces;
using Polly;

namespace CalculateFunding.Services.Results.UnitTests
{
    public static class ResultsResilienceTestHelper
    {
        public static IResultsResilliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                CalculationProviderResultsSearchRepository = Policy.NoOpAsync(),
                ResultsRepository = Policy.NoOpAsync(),
                ResultsSearchRepository = Policy.NoOpAsync(),
                SpecificationsRepository = Policy.NoOpAsync(),
                AllocationNotificationFeedSearchRepository = Policy.NoOpAsync(),
                ProviderProfilingRepository = Policy.NoOpAsync(),
                PublishedProviderCalculationResultsRepository = Policy.NoOpAsync(),
                PublishedProviderResultsRepository = Policy.NoOpAsync(),
                CalculationsRepository = Policy.NoOpAsync()
            };
        }
    }
}
