using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public interface IReProfilingStrategy
    {
        public string StrategyKey { get; }

        public string DisplayName { get; }

        public string Description { get; }

        ReProfileStrategyResult ReProfile(ReProfileContext context);
    }
}
