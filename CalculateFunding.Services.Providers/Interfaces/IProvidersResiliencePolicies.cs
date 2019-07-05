using Polly;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProvidersResiliencePolicies
    {
        Policy ProviderVersionsSearchRepository { get; set; }
        Policy ProviderVersionMetadataRepository { get; set; }
        Policy BlobRepositoryPolicy { get; set; }
    }
}
