namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsResiliencePolicies
    {
        Polly.AsyncPolicy JobsApiClient { get; set; }
        Polly.AsyncPolicy PoliciesApiClient { get; set; }

        Polly.AsyncPolicy CalcsApiClient { get; set; }

        Polly.AsyncPolicy ProvidersApiClient { get; set; }
    }
}
