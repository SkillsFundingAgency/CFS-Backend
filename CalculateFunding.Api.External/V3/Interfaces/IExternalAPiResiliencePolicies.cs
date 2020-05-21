using Polly;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IExternalApiResiliencePolicies
    {
        AsyncPolicy PublishedProviderBlobRepositoryPolicy { get; set; }

        AsyncPolicy PublishedFundingBlobRepositoryPolicy { get; set; }

        AsyncPolicy PublishedFundingRepositoryPolicy { get; set; }

        AsyncPolicy PoliciesApiClientPolicy { get; set; }
    }
}
