using System.Collections.Generic;
using CalculateFunding.Services.Profiling.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Profiling.Services
{
    public interface IReprofilingStrategyListService
    {
        ActionResult<IEnumerable<ReProfilingStrategyResponse>> GetAllStrategies();
    }
}