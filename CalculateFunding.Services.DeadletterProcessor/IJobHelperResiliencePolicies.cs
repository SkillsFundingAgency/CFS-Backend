namespace CalculateFunding.Services.DeadletterProcessor
{
    public interface IJobHelperResiliencePolicies
    {
        Polly.AsyncPolicy JobsApiClient { get; set; }
    }
}
