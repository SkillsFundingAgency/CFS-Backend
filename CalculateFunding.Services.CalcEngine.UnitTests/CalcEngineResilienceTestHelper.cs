using CalculateFunding.Services.CalcEngine.Interfaces;
using Polly;

namespace CalculateFunding.Services.CalcEngine.UnitTests
{
    public static class CalcEngineResilienceTestHelper
    {
        public static ICalculatorResiliencePolicies GenerateTestPolicies()
        {
            return new CalculatorResiliencePolicies()
            {
                CalculationsApiClient = Policy.NoOpAsync(),
                CalculationResultsRepository = Policy.NoOpAsync(),
                ProviderSourceDatasetsRepository = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                Messenger = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync(),
                ResultsApiClient = Policy.NoOpAsync()
            };
        }
    }
}
