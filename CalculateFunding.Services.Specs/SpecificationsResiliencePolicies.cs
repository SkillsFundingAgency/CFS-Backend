using CalculateFunding.Services.Specs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsResiliencePolicies : ISpecificationsResiliencePolicies
    {
        public Policy JobsApiClient { get; set; }

        public Policy PoliciesApiClient { get; set; }

        public Policy CalcsApiClient { get; set; }
    }
}
