namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsResiliencePolicies
    {
        Polly.AsyncPolicy JobsApiClient { get; set; }
        Polly.AsyncPolicy PoliciesApiClient { get; set; }

        Polly.AsyncPolicy CalcsApiClient { get; set; }

        Polly.AsyncPolicy ProvidersApiClient { get; set; }
        
        Polly.AsyncPolicy DatasetsApiClient { get; set; }
        
        Polly.AsyncPolicy SpecificationsSearchRepository { get; set; }
        
        Polly.AsyncPolicy SpecificationsRepository { get; set; }
        
        Polly.AsyncPolicy ResultsApiClient { get; set; }

        Polly.AsyncPolicy CacheProvider { get; set; }
    }
}
