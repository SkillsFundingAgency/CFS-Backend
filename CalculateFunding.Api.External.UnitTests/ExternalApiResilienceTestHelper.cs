using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Services;
using Polly;

namespace CalculateFunding.Api.External.UnitTests
{
    public static class ExternalApiResilienceTestHelper
    {
        public static IExternalApiResiliencePolicies GenerateTestPolicies()
        {
            return new ExternalApiResiliencePolicies()
            {
                PublishedProviderBlobRepositoryPolicy = Policy.NoOpAsync(),
                PublishedFundingBlobRepositoryPolicy = Policy.NoOpAsync(),
                PublishedFundingRepositoryPolicy = Policy.NoOpAsync()
            };
        }
    }
}
