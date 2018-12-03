using CalculateFunding.Services.Calcs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public static class CalcsResilienceTestHelper
    {
        public static ICalcsResilliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                CalculationsRepository = Policy.NoOpAsync(),
                CalculationsSearchRepository = Policy.NoOpAsync(),
                CacheProviderPolicy = Policy.NoOpAsync(),
                CalculationsVersionsRepositoryPolicy = Policy.NoOpAsync(),
                BuildProjectRepositoryPolicy = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                MessagePolicy = Policy.NoOpAsync(),
                JobsRepository = Policy.NoOpAsync()
            };
        }
    }
}
