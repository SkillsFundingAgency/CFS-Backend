using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class FundingPeriodController : ControllerBase
    {
        public FundingPeriodController()
        {

        }

        /// <summary>
        /// Get all funding periods
        /// </summary>
        /// <returns>A list of funding periods</returns>
        [HttpGet("api/fundingperiods")]
        [Produces(typeof(IEnumerable<Period>))]
        public async Task<IActionResult> GetFundingPeriods()
        {
            return new OkObjectResult(Enumerable.Empty<Period>());
        }

        /// <summary>
        /// Gets a specific funding period by ID
        /// </summary>
        /// <param name="fundingPeriodId">Funding Period ID eg AY1920</param>
        /// <returns>Funding Period</returns>
        [HttpGet("api/fundingperiods/{fundingPeriodId}")]
        [Produces(typeof(Period))]
        public async Task<IActionResult> GetFundingPeriodById([FromRoute]string fundingPeriodId)
        {
            return new OkObjectResult(new Period());
        }

        /// <summary>
        /// Saves (creates or updates) a Funding Period
        /// </summary>
        /// <param name="fundingPeriodYaml">YAML contents of a funding period</param>
        /// <returns>Saved Funding Period</returns>
        [HttpPost("api/fundingperiods")]
        [Produces(typeof(Period))]
        public async Task<IActionResult> SaveFundingPeriod([FromBody]string fundingPeriodYaml)
        {
            return new OkObjectResult(new Period());
        }
    }
}
