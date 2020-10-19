using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Profiling.Controllers
{
    [Authorize(Roles = "ExecuteApi")]
    [ApiController]
    public class ReProfilingController : ControllerBase
    {
        private readonly IReprofilingStrategyListService _reProfilingStrategyListService;
        private readonly IReprofilingService _reProfilingService;

        public ReProfilingController(IReprofilingStrategyListService reProfilingStrategyListService,
            IReprofilingService reProfilingService)
        {
            Guard.ArgumentNotNull(reProfilingStrategyListService, nameof(reProfilingStrategyListService));
            Guard.ArgumentNotNull(reProfilingService, nameof(reProfilingService));
            
            _reProfilingStrategyListService = reProfilingStrategyListService;
            _reProfilingService = reProfilingService;
        }

        /// <summary>
        ///     Re-profile an allocation amount based on a profile pattern and existing values for funding stream and period
        /// </summary>
        [Route("api/reprofile")]
        [HttpPost]
        public async Task<ActionResult<ReProfileResponse>> Post([FromBody] ReProfileRequest request)
            => await _reProfilingService.ReProfile(request);

        /// <summary>
        ///     List all available re-profiling strategies
        /// </summary>
        [Route("api/reprofilingstrategies")]
        [HttpGet]
        public ActionResult<IEnumerable<ReProfilingStrategyResponse>> GetAllReProfilingStrategies()
            => _reProfilingStrategyListService.GetAllStrategies();
    }
}