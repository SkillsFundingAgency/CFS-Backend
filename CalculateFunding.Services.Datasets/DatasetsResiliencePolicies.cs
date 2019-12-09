using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Datasets.Interfaces;
using Polly;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetsResiliencePolicies : IDatasetsResiliencePolicies, IJobHelperResiliencePolicies
    {
        public Policy ProviderRepository { get; set; }
   
        public Policy ProviderResultsRepository { get; set; }

        public Policy DatasetRepository { get; set; }

        public Policy DatasetSearchService { get; set; }

        public Policy SpecificationsApiClient { get; set; }

        public Policy CacheProviderRepository { get; set; }

        public Policy DatasetDefinitionSearchRepository { get; set; }

        public Policy BlobClient { get; set; }

        public Policy JobsApiClient { get; set; }

        public Policy ProvidersApiClient { get; set; }
    }
}
