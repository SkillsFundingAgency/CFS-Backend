using CalculateFunding.Services.Providers.Interfaces;
using Polly;

namespace CalculateFunding.Services.Providers
{
    public class ProvidersResiliencePolicies : IProvidersResiliencePolicies
    {
        public Policy ProviderVersionsSearchRepository { get; set; }

        public Policy ProviderVersionRepository { get; set; }

        public Policy ProviderVersionMetadataRepository { get; set; }
    }
}
