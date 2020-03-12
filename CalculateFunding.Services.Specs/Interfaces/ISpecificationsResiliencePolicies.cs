namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsResiliencePolicies
    {
        Polly.Policy JobsApiClient { get; set; }
        Polly.Policy PoliciesApiClient { get; set; }

        Polly.Policy CalcsApiClient { get; set; }

        Polly.Policy ProvidersApiClient { get; set; }
    }
}
