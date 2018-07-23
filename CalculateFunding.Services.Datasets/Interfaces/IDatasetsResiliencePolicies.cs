using Polly;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetsResiliencePolicies
    {
        Policy ProviderResultsRepository { get; set; }

        Policy ProviderRepository { get; set; }

        Policy DatasetRepository { get; set; }

        Policy DatasetSearchService { get; set; }

        Policy SpecificationsRepository { get; set; }

        Policy CacheProviderRepository { get; set; }

        Policy DatasetDefinitionSearchRepository { get; set; }
    }
}
