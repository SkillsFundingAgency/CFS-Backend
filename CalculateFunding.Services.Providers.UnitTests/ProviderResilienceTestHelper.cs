using CalculateFunding.Services.Providers.Interfaces;
using Polly;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public static class ProviderResilienceTestHelper
    {
        public static IProvidersResiliencePolicies GenerateTestPolicies()
        {
            return new ProvidersResiliencePolicies()
            {
                ProviderVersionsSearchRepository = Policy.NoOpAsync(),
                ProviderVersionMetadataRepository = Policy.NoOpAsync(),
                BlobRepositoryPolicy = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync()
            };
        }
    }
}
