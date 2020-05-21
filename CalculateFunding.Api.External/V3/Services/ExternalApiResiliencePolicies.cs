using CalculateFunding.Api.External.V3.Interfaces;
using Polly;

namespace CalculateFunding.Api.External.V3.Services
{
    public class ExternalApiResiliencePolicies : IExternalApiResiliencePolicies
    {
        public AsyncPolicy PublishedProviderBlobRepositoryPolicy { get; set; }

        public AsyncPolicy PublishedFundingBlobRepositoryPolicy { get; set; }

        public AsyncPolicy PublishedFundingRepositoryPolicy { get; set; }
        public AsyncPolicy PoliciesApiClientPolicy { get; set; }
    }
}
