using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Polly;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public static class CosmosDbScalingResilienceTestHelper
    {
        public static ICosmosDbScallingResilliencePolicies GenerateTestPolicies()
        {
            return new CosmosDbScallingResilliencePolicies()
            {
                 CacheProvider = Policy.NoOpAsync(),
                 JobsApiClient = Policy.NoOpAsync(),
                 ScalingConfigRepository = Policy.NoOpAsync(),
                 ScalingRepository = Policy.NoOpAsync()
            };
        }
    }
}
