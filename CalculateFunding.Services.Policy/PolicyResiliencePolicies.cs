using CalculateFunding.Services.Policy.Interfaces;
using Polly;

namespace CalculateFunding.Services.Policy
{
    public class PolicyResiliencePolicies : IPolicyResiliencePolicies
    {
        public Polly.AsyncPolicy PolicyRepository { get; set; }

        public Polly.AsyncPolicy CacheProvider { get; set; }

        public Polly.AsyncPolicy FundingSchemaRepository { get; set; }

        public Polly.AsyncPolicy FundingTemplateRepository { get; set; }
    }
}
