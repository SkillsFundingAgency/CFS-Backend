using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class FundingPeriodController : ControllerBase
    {
        private readonly IFundingPeriodService _fundingPeriodService;

        public FundingPeriodController(IFundingPeriodService fundingPeriodService)
        {
            _fundingPeriodService = fundingPeriodService;
        }

        /// <summary>
        /// Get all funding periods
        /// </summary>
        /// <returns>A list of funding periods</returns>
        [HttpGet("api/fundingperiods")]
        [Produces(typeof(IEnumerable<FundingPeriod>))]
        public async Task<IActionResult> GetFundingPeriods()
        {
            return await _fundingPeriodService.GetFundingPeriods();
        }

        /// <summary>
        /// Gets a specific funding period by ID
        /// </summary>
        /// <param name="fundingPeriodId">Funding Period ID eg AY1920</param>
        /// <returns>Funding Period</returns>
        [HttpGet("api/fundingperiods/{fundingPeriodId}")]
        [Produces(typeof(FundingPeriod))]
        public async Task<IActionResult> GetFundingPeriodById([FromRoute]string fundingPeriodId)
        {
            return await _fundingPeriodService.GetFundingPeriodById(fundingPeriodId);
        }

        /// <summary> 
        /// Saves (creates or updates) a Funding Period
        /// </summary>
        /// <returns>Saved Funding Period</returns>
        [HttpPost("api/fundingperiods")]
        [Produces(typeof(FundingPeriod))]
        public async Task<IActionResult> SaveFundingPeriods([FromBody] FundingPeriodsJsonModel fundingPeriodsJsonModel)
        {
            return await _fundingPeriodService.SaveFundingPeriods(fundingPeriodsJsonModel);
        }
    }
}
