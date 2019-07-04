using CalculateFunding.Services.Policy.Interfaces;

namespace CalculateFunding.Services.Policy.UnitTests
{
    public static class PolicyResilliencePoliciesTestHelper
    {
        public static IPolicyResilliencePolicies GenerateTestPolicies()
        {
            return new PolicyResilliencePolicies()
            {
                PolicyRepository = Polly.Policy.NoOpAsync(),
                CacheProvider = Polly.Policy.NoOpAsync(),
                FundingSchemaRepository = Polly.Policy.NoOpAsync(),
                FundingTemplateRepository = Polly.Policy.NoOpAsync()
            };
        }
    }
}
