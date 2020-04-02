using Polly;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetsResiliencePolicies
    {
        AsyncPolicy ProviderResultsRepository { get; set; }

        AsyncPolicy ProviderRepository { get; set; }

        AsyncPolicy DatasetRepository { get; set; }

        AsyncPolicy DatasetSearchService { get; set; }

        AsyncPolicy SpecificationsApiClient { get; set; }

        AsyncPolicy CacheProviderRepository { get; set; }

        AsyncPolicy DatasetDefinitionSearchRepository { get; set; }

        AsyncPolicy BlobClient { get; set; }

        AsyncPolicy JobsApiClient { get; set; }

        AsyncPolicy ProvidersApiClient { get; set; }
    }
}
