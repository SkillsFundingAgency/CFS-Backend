using Polly;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProvidersResiliencePolicies
    {
        AsyncPolicy ProviderVersionsSearchRepository { get; set; }
        AsyncPolicy ProviderVersionMetadataRepository { get; set; }
        AsyncPolicy BlobRepositoryPolicy { get; set; }
    }
}
