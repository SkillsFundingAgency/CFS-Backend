using CalculateFunding.Services.Scenarios;
using CalculateFunding.Services.Scenarios.Interfaces;
using Polly;

namespace CalculateFunding.Services.Scenarios
{
    public static class ScenariosResilienceTestHelper
    {
        public static IScenariosResiliencePolicies GenerateTestPolicies()
        {
            return new ScenariosResiliencePolicies()
            {
                CalcsRepository = Policy.NoOpAsync(),
                JobsRepository = Policy.NoOpAsync()
            };
        }
    }
}
