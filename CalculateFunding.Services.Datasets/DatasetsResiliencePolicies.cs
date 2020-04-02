using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.DeadletterProcessor;
using Polly;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetsResiliencePolicies : IDatasetsResiliencePolicies, IJobHelperResiliencePolicies
    {
        public AsyncPolicy ProviderRepository { get; set; }

        public AsyncPolicy ProviderResultsRepository { get; set; }

        public AsyncPolicy DatasetRepository { get; set; }

        public AsyncPolicy DatasetSearchService { get; set; }

        public AsyncPolicy SpecificationsApiClient { get; set; }

        public AsyncPolicy CacheProviderRepository { get; set; }

        public AsyncPolicy DatasetDefinitionSearchRepository { get; set; }

        public AsyncPolicy BlobClient { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }

        public AsyncPolicy ProvidersApiClient { get; set; }
    }
}
