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
                JobsApiClient = Policy.NoOpAsync(),
                BlobClient = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                FundingFeedSearchRepository = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync(),
                PublishedFundingRepository = Policy.NoOpAsync(),
                ProfilingApiClient = Policy.NoOpAsync(),
                CalculationResultsRepository = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                PublishedIndexSearchResiliencePolicy = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                DatasetsApiClient = Policy.NoOpAsync()
            };
        }
    }
}
