using System.Threading.Tasks;
using CalculateFunding.Models.FundingPolicy;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class FundingConfigurationController : ControllerBase
    {
        public FundingConfigurationController()
        {

        }

        [HttpGet("api/configuration/{fundingStreamId}/{fundingPeriodId}")]
        [Produces(typeof(FundingConfiguration))]
        public async Task<IActionResult> GetFundingConfiguration([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId)
        {
            return new OkObjectResult(new FundingConfiguration());
        }



        [HttpPost("api/configuration/{fundingStreamId}/{fundingPeriodId}")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SaveFundingConfiguration([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId, [FromBody]FundingConfigurationUpdateViewModel configuration)
        {
            return new OkObjectResult(new FundingConfiguration());
        }
    }
}
