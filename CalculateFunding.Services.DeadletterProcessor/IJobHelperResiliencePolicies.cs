namespace CalculateFunding.Services.DeadletterProcessor
{
    public interface IJobHelperResiliencePolicies
    {
        Polly.Policy JobsApiClient { get; set; }
    }
}
