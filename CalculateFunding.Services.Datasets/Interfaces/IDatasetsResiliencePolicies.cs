﻿using Polly;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetsResiliencePolicies
    {
        Policy ProviderResultsRepository { get; set; }

        Policy ProviderRepository { get; set; }

        Policy DatasetRepository { get; set; }

        Policy DatasetSearchService { get; set; }

        Policy SpecificationsApiClient { get; set; }

        Policy CacheProviderRepository { get; set; }

        Policy DatasetDefinitionSearchRepository { get; set; }

        Policy BlobClient { get; set; }

        Policy JobsApiClient { get; set; }

        Policy ProvidersApiClient { get; set; }
    }
}
