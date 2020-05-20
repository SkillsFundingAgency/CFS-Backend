using CalculateFunding.Services.DeadletterProcessor;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IPolicyResiliencePolicies : IJobHelperResiliencePolicies
    {
        Polly.AsyncPolicy PolicyRepository { get; set; }

        Polly.AsyncPolicy CacheProvider { get; set; }

        Polly.AsyncPolicy FundingSchemaRepository { get; set; }

        Polly.AsyncPolicy FundingTemplateRepository { get; set; }

        Polly.AsyncPolicy TemplatesSearchRepository { get; set; }
        
        Polly.AsyncPolicy TemplatesRepository { get; set; }
    }
}
