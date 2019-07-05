namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IPolicyResiliencePolicies
    {
        Polly.Policy PolicyRepository { get; set; }

        Polly.Policy CacheProvider { get; set; }

        Polly.Policy FundingSchemaRepository { get; set; }

        Polly.Policy FundingTemplateRepository { get; set; }
    }
}
