using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.Services;
using Polly;

namespace CalculateFunding.Api.External.UnitTests.Version4
{
    public static class ExternalApiResilienceTestHelper
    {
        public static IExternalApiResiliencePolicies GenerateTestPolicies()
        {
            return new ExternalApiResiliencePolicies()
            {
                PublishedProviderBlobRepositoryPolicy = Policy.NoOpAsync(),
                PublishedFundingBlobRepositoryPolicy = Policy.NoOpAsync(),
                PoliciesApiClientPolicy = Policy.NoOpAsync()
            };
        }
    }
}
