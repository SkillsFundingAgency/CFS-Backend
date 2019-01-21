namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsResiliencePolicies
    {
        Polly.Policy JobsApiClient { get; set; }
    }
}
