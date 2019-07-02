using CalculateFunding.Services.Policy.Interfaces;

namespace CalculateFunding.Services.Policy
{
    public class PolicyResilliencePolicies : IPolicyResilliencePolicies
    {
        public Polly.Policy PolicyRepository { get; set; }

        public Polly.Policy CacheProvider { get; set; }

        public Polly.Policy FundingSchemaRepository { get; set; }
    }
}
