using Polly;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface IGraphResiliencePolicies
    {
        AsyncPolicy CacheProviderPolicy { get; set; }
    }
}
