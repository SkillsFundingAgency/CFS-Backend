using Polly;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsResiliencePolicies
    {
        AsyncPolicy JobsApiClient { get; set; }
        
        AsyncPolicy PoliciesApiClient { get; set; }

        AsyncPolicy CalcsApiClient { get; set; }

        AsyncPolicy ProvidersApiClient { get; set; }
        
        AsyncPolicy DatasetsApiClient { get; set; }
        
        AsyncPolicy SpecificationsSearchRepository { get; set; }
        
        AsyncPolicy SpecificationsRepository { get; set; }
        
        AsyncPolicy ResultsApiClient { get; set; }

        AsyncPolicy CacheProvider { get; set; }
        
        AsyncPolicy GraphApiClient { get; set; }
        
        
    }
}
