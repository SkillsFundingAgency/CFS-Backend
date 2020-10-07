using Polly;

namespace CalculateFunding.Services.Profiling.Services
{
    public interface IProfilingResiliencePolicies
    {
        AsyncPolicy ProfilePatternRepository { get; set; }
        
        AsyncPolicy Caching { get; set; }
    }
}