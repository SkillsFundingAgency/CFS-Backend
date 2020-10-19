using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class ReProfilingStrategyLocator : IReProfilingStrategyLocator
    {
        private readonly Dictionary<string, IReProfilingStrategy> _strategies;

        public ReProfilingStrategyLocator(IEnumerable<IReProfilingStrategy> reProfilingStrategies)
        {
            _strategies = reProfilingStrategies.ToDictionary(_ => _.StrategyKey);
        }

        public IReProfilingStrategy GetStrategy(string strategyKey)
            => _strategies.TryGetValue(strategyKey, out IReProfilingStrategy result) ? result : null;

        public IEnumerable<IReProfilingStrategy> GetAllStrategies() => _strategies.Values;

        public bool HasStrategy(string key)
            => _strategies?.ContainsKey(key) == true;
    }
}