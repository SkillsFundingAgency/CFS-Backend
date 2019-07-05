using CalculateFunding.Services.Providers.Interfaces;
using Polly;

namespace CalculateFunding.Services.Providers
{
    public class ProvidersResiliencePolicies : IProvidersResiliencePolicies
    {
        public Policy ProviderVersionsSearchRepository { get; set; }

        public Policy ProviderVersionMetadataRepository { get; set; }

        public Policy BlobRepositoryPolicy { get; set; }
    }
}
