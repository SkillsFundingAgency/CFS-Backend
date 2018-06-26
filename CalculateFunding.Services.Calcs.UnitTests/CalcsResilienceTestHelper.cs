using CalculateFunding.Services.Calcs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public static class CalcsResilienceTestHelper
    {
        public static ICalcsResilliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                CalculationsRepository = Policy.NoOpAsync()
            };
        }
    }
}
