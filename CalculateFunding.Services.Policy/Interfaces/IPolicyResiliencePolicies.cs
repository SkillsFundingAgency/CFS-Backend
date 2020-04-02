namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IPolicyResiliencePolicies
    {
        Polly.AsyncPolicy PolicyRepository { get; set; }

        Polly.AsyncPolicy CacheProvider { get; set; }

        Polly.AsyncPolicy FundingSchemaRepository { get; set; }

        Polly.AsyncPolicy FundingTemplateRepository { get; set; }
    }
}
