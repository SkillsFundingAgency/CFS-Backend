using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Api.External.UnitTests
{
    public static class PublishingResilienceTestHelper
    {
        public static IPublishingResiliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                PublishedProviderVersionRepository = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync(),
                BlobClient = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync(),
                PublishedFundingRepository = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
            };
        }
    }
}
