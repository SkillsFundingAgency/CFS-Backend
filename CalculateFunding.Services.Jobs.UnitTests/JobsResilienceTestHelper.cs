using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public static class JobsResilienceTestHelper
    {
        public static IJobsResilliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                JobDefinitionsRepository = Policy.NoOpAsync(),
                CacheProviderPolicy = Policy.NoOpAsync()
            };
        }
    }
}
