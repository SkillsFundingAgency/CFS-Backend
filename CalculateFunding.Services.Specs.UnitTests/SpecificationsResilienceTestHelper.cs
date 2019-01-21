using CalculateFunding.Services.Specs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public static class SpecificationsResilienceTestHelper
    {
        public static ISpecificationsResiliencePolicies GenerateTestPolicies()
        {
            return new SpecificationsResiliencePolicies()
            {
                JobsApiClient = Policy.NoOpAsync()
            };
        }
    }
}
