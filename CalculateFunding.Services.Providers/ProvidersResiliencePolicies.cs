using CalculateFunding.Services.Providers.Interfaces;
using Polly;

namespace CalculateFunding.Services.Providers
{
    public class ProvidersResiliencePolicies : IProvidersResiliencePolicies
    {
        public AsyncPolicy ProviderVersionsSearchRepository { get; set; }

        public AsyncPolicy ProviderVersionMetadataRepository { get; set; }

        public AsyncPolicy BlobRepositoryPolicy { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }
    }
}
