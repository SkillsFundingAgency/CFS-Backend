using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Profiling.Services
{
    public class ReProfilingStrategyListService : IReprofilingStrategyListService
    {
        private readonly IReProfilingStrategyLocator _reProfilingStrategyLocator;

        public ReProfilingStrategyListService(IReProfilingStrategyLocator reProfilingStrategyLocator)
        {
            Guard.ArgumentNotNull(reProfilingStrategyLocator, nameof(reProfilingStrategyLocator));

            _reProfilingStrategyLocator = reProfilingStrategyLocator;
        }

        public ActionResult<IEnumerable<ReProfilingStrategyResponse>> GetAllStrategies()
        {
            IEnumerable<IReProfilingStrategy> strategies = _reProfilingStrategyLocator.GetAllStrategies();

            return strategies.Select(_ => new ReProfilingStrategyResponse
            {
                StrategyKey = _.StrategyKey,
                Description = _.Description,
                DisplayName = _.DisplayName
            }).ToList();
        }
    }
}