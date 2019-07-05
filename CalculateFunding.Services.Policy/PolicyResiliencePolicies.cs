using CalculateFunding.Services.Policy.Interfaces;
using Polly;

namespace CalculateFunding.Services.Policy
{
    public class PolicyResiliencePolicies : IPolicyResiliencePolicies
    {
        public Polly.Policy PolicyRepository { get; set; }

        public Polly.Policy CacheProvider { get; set; }

        public Polly.Policy FundingSchemaRepository { get; set; }

        public Polly.Policy FundingTemplateRepository { get; set; }
    }
}
