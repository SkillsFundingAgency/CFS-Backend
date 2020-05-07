using CalculateFunding.Services.Calcs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public static class CalcsResilienceTestHelper
    {
        public static ICalcsResiliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                CalculationsRepository = Policy.NoOpAsync(),
                CalculationsSearchRepository = Policy.NoOpAsync(),
                CacheProviderPolicy = Policy.NoOpAsync(),
                CalculationsVersionsRepositoryPolicy = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                MessagePolicy = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
                SourceFilesRepository = Policy.NoOpAsync(),
                DatasetsRepository = Policy.NoOpAsync(),
                BuildProjectRepositoryPolicy = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
            };
        }
    }
}
