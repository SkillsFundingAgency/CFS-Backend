using Polly;

namespace CalculateFunding.Services.Profiling.Services
{
    public class ProfilingResiliencePolicies : IProfilingResiliencePolicies
    {
        public AsyncPolicy ProfilePatternRepository { get; set; }   
        
        public AsyncPolicy Caching { get; set; }   
    }
}