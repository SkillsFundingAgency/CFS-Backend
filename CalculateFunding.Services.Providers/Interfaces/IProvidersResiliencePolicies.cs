﻿using Polly;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProvidersResiliencePolicies
    {
        AsyncPolicy ProviderVersionsSearchRepository { get; set; }
        AsyncPolicy ProviderVersionMetadataRepository { get; set; }
        AsyncPolicy BlobRepositoryPolicy { get; set; }
        AsyncPolicy JobsApiClient { get; set; }
        AsyncPolicy PoliciesApiClient { get; set; }
        AsyncPolicy SpecificationsApiClient { get; set; }
        AsyncPolicy ResultsApiClient { get; set; }
        AsyncPolicy CacheProvider { get; set; }
        AsyncPolicy FundingDataZoneApiClient { get; set; }
    }
}
