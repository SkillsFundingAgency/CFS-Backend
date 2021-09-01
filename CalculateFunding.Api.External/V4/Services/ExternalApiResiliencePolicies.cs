using CalculateFunding.Api.External.V4.Interfaces;
using Polly;

namespace CalculateFunding.Api.External.V4.Services
{
    public class ExternalApiResiliencePolicies : IExternalApiResiliencePolicies
    {
        public AsyncPolicy PublishedProviderBlobRepositoryPolicy { get; set; }

        public AsyncPolicy PublishedFundingBlobRepositoryPolicy { get; set; }

        public AsyncPolicy PoliciesApiClientPolicy { get; set; }
    }
}
