using Polly;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingResiliencePolicies
    {
        AsyncPolicy ScalingRepository { get; set; }

        AsyncPolicy ScalingConfigRepository { get; set; }

        AsyncPolicy JobsApiClient { get; set; }

        AsyncPolicy CacheProvider { get; set; }
    }
}
