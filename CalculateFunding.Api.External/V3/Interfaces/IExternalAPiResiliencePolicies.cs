using Polly;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IExternalApiResiliencePolicies
    {
        Policy BlobRepositoryPolicy { get; set; }
    }
}
