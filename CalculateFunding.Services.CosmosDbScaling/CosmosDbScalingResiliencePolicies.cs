using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Polly;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbScalingResiliencePolicies : ICosmosDbScalingResiliencePolicies
    {
        public AsyncPolicy ScalingRepository { get; set; }

        public AsyncPolicy ScalingConfigRepository { get; set; }

        public AsyncPolicy JobsApiClient { get; set; }

        public AsyncPolicy CacheProvider { get; set; }
    }
}
