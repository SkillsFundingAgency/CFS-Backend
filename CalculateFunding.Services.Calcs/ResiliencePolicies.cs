using CalculateFunding.Services.Calcs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public class ResiliencePolicies : ICalcsResiliencePolicies
    {
        public AsyncPolicy GraphApiClientPolicy { get; set; }
        
        public AsyncPolicy CalculationsRepository { get; set; }

        public AsyncPolicy CalculationsSearchRepository { get; set; }

        public AsyncPolicy CacheProviderPolicy { get; set; }

        public AsyncPolicy CalculationsVersionsRepositoryPolicy { get; set; }

        public AsyncPolicy BuildProjectRepositoryPolicy { get; set; }

        public AsyncPolicy SpecificationsRepositoryPolicy { get; set; }

        public AsyncPolicy MessagePolicy { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }

        public AsyncPolicy ProvidersApiClient { get; set; }

        public AsyncPolicy SourceFilesRepository { get; set; }

        public AsyncPolicy PoliciesApiClient { get; set; }

        public AsyncPolicy SpecificationsApiClient { get; set; }

        public AsyncPolicy DatasetsApiClient { get; set; }
        public AsyncPolicy ResultsApiClient { get; set; }
    }
}
