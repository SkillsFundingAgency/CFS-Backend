using CalculateFunding.Services.Specs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsResiliencePolicies : ISpecificationsResiliencePolicies
    {
        public AsyncPolicy JobsApiClient { get; set; }

        public AsyncPolicy PoliciesApiClient { get; set; }

        public AsyncPolicy CalcsApiClient { get; set; }

        public AsyncPolicy ProvidersApiClient { get; set; }

    }
}
