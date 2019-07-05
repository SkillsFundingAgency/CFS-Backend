using Polly;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbScalingResiliencePolicies
    {
        Policy ScalingRepository { get; set; }

        Policy ScalingConfigRepository { get; set; }

        Policy JobsApiClient { get; set; }

        Policy CacheProvider { get; set; }
    }
}
