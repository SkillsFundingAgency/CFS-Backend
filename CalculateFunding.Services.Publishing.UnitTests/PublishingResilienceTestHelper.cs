
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public static class PublishingResilienceTestHelper
    {
        public static IPublishingResiliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                PublishedProviderVersionRepository = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync()
            };
        }
    }
}
