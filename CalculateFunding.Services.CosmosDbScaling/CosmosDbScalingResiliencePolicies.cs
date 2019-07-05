using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Polly;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbScalingResiliencePolicies : ICosmosDbScalingResiliencePolicies
    {
        public Policy ScalingRepository { get; set; }

        public Policy ScalingConfigRepository { get; set; }

        public Policy JobsApiClient { get; set; }

        public Policy CacheProvider { get; set; }
    }
}
