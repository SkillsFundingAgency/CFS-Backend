using CalculateFunding.Services.Datasets.Interfaces;
using Polly;

namespace CalculateFunding.Services.Datasets
{
    public static class DatasetsResilienceTestHelper
    {
        public static IDatasetsResiliencePolicies GenerateTestPolicies()
        {
            return new DatasetsResiliencePolicies()
            {
                DatasetRepository = Policy.NoOpAsync(),
                CacheProviderRepository = Policy.NoOpAsync(),
                ProviderResultsRepository = Policy.NoOpAsync(),
                DatasetSearchService = Policy.NoOpAsync(),
                ProviderRepository = Policy.NoOpAsync(),
                SpecificationsRepository = Policy.NoOpAsync(),
                DatasetDefinitionSearchRepository = Policy.NoOpAsync(),
                BlobClient = Policy.NoOpAsync()
            };
        }
    }
}
