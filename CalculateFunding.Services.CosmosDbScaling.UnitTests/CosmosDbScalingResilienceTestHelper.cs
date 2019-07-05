using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Polly;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public static class CosmosDbScalingResilienceTestHelper
    {
        public static ICosmosDbScalingResiliencePolicies GenerateTestPolicies()
        {
            return new CosmosDbScalingResiliencePolicies()
            {
                 CacheProvider = Policy.NoOpAsync(),
                 JobsApiClient = Policy.NoOpAsync(),
                 ScalingConfigRepository = Policy.NoOpAsync(),
                 ScalingRepository = Policy.NoOpAsync()
            };
        }
    }
}
