using Polly;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalcsResiliencePolicies
    {
        AsyncPolicy GraphApiClientPolicy { get; set; }

        AsyncPolicy CalculationsRepository { get; set; }
        
        AsyncPolicy CalculationsRepositoryNoOCCRetry { get; set; }

        AsyncPolicy CalculationsSearchRepository { get; set; }

        AsyncPolicy CacheProviderPolicy { get; set; }

        AsyncPolicy CalculationsVersionsRepositoryPolicy { get; set; }

        AsyncPolicy BuildProjectRepositoryPolicy { get; set; }

        AsyncPolicy SpecificationsRepositoryPolicy { get; set; }

        AsyncPolicy MessagePolicy { get; set; }

        AsyncPolicy JobsApiClient { get; set; }

        AsyncPolicy ProvidersApiClient { get; set; }

        AsyncPolicy SourceFilesRepository { get; set; }

        AsyncPolicy PoliciesApiClient { get; set; }

        AsyncPolicy SpecificationsApiClient { get; set; }

        AsyncPolicy DatasetsApiClient { get; set; }

        AsyncPolicy ResultsApiClient { get; set; }

        AsyncPolicy CalcEngineApiClient { get; set; }
    }
}
