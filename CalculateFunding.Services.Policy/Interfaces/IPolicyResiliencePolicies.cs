using CalculateFunding.Services.DeadletterProcessor;
using Polly;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IPolicyResiliencePolicies : IJobHelperResiliencePolicies
    {
        AsyncPolicy PolicyRepository { get; set; }

        AsyncPolicy CacheProvider { get; set; }

        AsyncPolicy FundingSchemaRepository { get; set; }

        AsyncPolicy FundingTemplateRepository { get; set; }

        AsyncPolicy TemplatesSearchRepository { get; set; }
        
        AsyncPolicy SpecificationsApiClient { get; set; }
        
        AsyncPolicy ResultsApiClient { get; set; }

        AsyncPolicy CalculationsApiClient { get; set; }
        
        AsyncPolicy TemplatesRepository { get; set; }
    }
}
