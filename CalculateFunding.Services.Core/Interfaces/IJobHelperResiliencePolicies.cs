namespace CalculateFunding.Services.Core.Interfaces
{
    public interface IJobHelperResiliencePolicies
    {
        Polly.Policy JobsApiClient { get; set; }
    }
}
