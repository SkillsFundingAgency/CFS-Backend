using CalculateFunding.Services.Policy.Interfaces;

namespace CalculateFunding.Services.Policy.UnitTests
{
    public static class PolicyResiliencePoliciesTestHelper
    {
        public static IPolicyResiliencePolicies GenerateTestPolicies()
        {
            return new PolicyResiliencePolicies()
            {
                PolicyRepository = Polly.Policy.NoOpAsync(),
                CacheProvider = Polly.Policy.NoOpAsync(),
                FundingSchemaRepository = Polly.Policy.NoOpAsync(),
                FundingTemplateRepository = Polly.Policy.NoOpAsync(),
                TemplatesSearchRepository = Polly.Policy.NoOpAsync(),
                JobsApiClient = Polly.Policy.NoOpAsync(),
                TemplatesRepository = Polly.Policy.NoOpAsync()
            };
        }
    }
}
