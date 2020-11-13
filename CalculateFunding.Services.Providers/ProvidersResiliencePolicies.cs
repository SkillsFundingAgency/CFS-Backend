using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Providers.Interfaces;
using Polly;

namespace CalculateFunding.Services.Providers
{
    public class ProvidersResiliencePolicies : IProvidersResiliencePolicies, IJobManagementResiliencePolicies
    {
        public AsyncPolicy ProviderVersionsSearchRepository { get; set; }

        public AsyncPolicy ProviderVersionMetadataRepository { get; set; }

        public AsyncPolicy BlobRepositoryPolicy { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }
        
        public AsyncPolicy PoliciesApiClient { get; set; }
        
        public AsyncPolicy SpecificationsApiClient { get; set; }
        
        public AsyncPolicy ResultsApiClient { get; set; }
        
        public AsyncPolicy CacheProvider { get; set; }

        public AsyncPolicy FundingDataZoneApiClient { get; set; }
    }
}

