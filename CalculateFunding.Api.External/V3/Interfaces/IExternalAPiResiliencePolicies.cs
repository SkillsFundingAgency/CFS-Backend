using Polly;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IExternalApiResiliencePolicies
    {
        Policy PublishedProviderBlobRepositoryPolicy { get; set; }

        Policy PublishedFundingBlobRepositoryPolicy { get; set; }
    }
}
