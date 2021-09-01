using Polly;

namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IExternalApiResiliencePolicies
    {
        AsyncPolicy PublishedProviderBlobRepositoryPolicy { get; set; }

        AsyncPolicy PublishedFundingBlobRepositoryPolicy { get; set; }


        AsyncPolicy PoliciesApiClientPolicy { get; set; }
    }
}
