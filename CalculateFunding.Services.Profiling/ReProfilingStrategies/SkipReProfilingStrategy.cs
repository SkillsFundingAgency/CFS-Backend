using CalculateFunding.Services.Profiling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Profiling.ReProfilingStrategies
{
    public class SkipReProfilingStrategy : ReProfilingStrategy, IReProfilingStrategy
    {
        public string StrategyKey => nameof(SkipReProfilingStrategy);

        public string DisplayName => "Used to skip reprofiling for targeted strategy.";

        public string Description => "Used to skip reprofiling for targeted strategy.";

        public ReProfileStrategyResult ReProfile(ReProfileContext context)
        {
            return new ReProfileStrategyResult
            {
                SkipReProfiling = true
            };
        }
    }
}
