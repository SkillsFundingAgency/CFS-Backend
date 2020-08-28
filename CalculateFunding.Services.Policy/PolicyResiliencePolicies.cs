using CalculateFunding.Services.Policy.Interfaces;
using Polly;

namespace CalculateFunding.Services.Policy
{
    public class PolicyResiliencePolicies : IPolicyResiliencePolicies
    {
        public AsyncPolicy PolicyRepository { get; set; }

        public AsyncPolicy CacheProvider { get; set; }

        public AsyncPolicy FundingSchemaRepository { get; set; }

        public AsyncPolicy FundingTemplateRepository { get; set; }

        public AsyncPolicy TemplatesSearchRepository { get; set; }
        
        public AsyncPolicy SpecificationsApiClient { get; set; }
        
        public AsyncPolicy ResultsApiClient { get; set; }
        
        public AsyncPolicy CalculationsApiClient { get; set; }
        
        public AsyncPolicy TemplatesRepository { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }
    }
}
