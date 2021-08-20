using CalculateFunding.Services.Datasets.Interfaces;
using Polly;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetsResiliencePolicies : IDatasetsResiliencePolicies
    {
        public AsyncPolicy ProviderRepository { get; set; }

        public AsyncPolicy ProviderResultsRepository { get; set; }

        public AsyncPolicy DatasetRepository { get; set; }

        public AsyncPolicy DatasetSearchService { get; set; }

        public AsyncPolicy DatasetVersionSearchService { get; set; }

        public AsyncPolicy SpecificationsApiClient { get; set; }

        public AsyncPolicy CacheProviderRepository { get; set; }

        public AsyncPolicy DatasetDefinitionSearchRepository { get; set; }

        public AsyncPolicy BlobClient { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }

        public AsyncPolicy ProvidersApiClient { get; set; }

        public AsyncPolicy GraphApiClient { get; set; }

        public AsyncPolicy PoliciesApiClient { get; set; }

        public AsyncPolicy CalculationsApiClient { get; set; }

        public AsyncPolicy RelationshipVersionRepository {get; set;}
    }
}
