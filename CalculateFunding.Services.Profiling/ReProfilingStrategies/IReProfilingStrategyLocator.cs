using System.Collections.Generic;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public interface IReProfilingStrategyLocator
    {
        IEnumerable<IReProfilingStrategy> GetAllStrategies();

        IReProfilingStrategy GetStrategy(string strategyKey);
        
        bool HasStrategy(string key);
    }
}