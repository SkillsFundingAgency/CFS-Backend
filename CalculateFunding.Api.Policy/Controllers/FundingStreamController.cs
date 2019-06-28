using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class FundingStreamController : ControllerBase
    {
        public FundingStreamController()
        {

        }

        [HttpGet("api/fundingstreams")]
        [Produces(typeof(IEnumerable<FundingStream>))]
        public async Task<IActionResult> GetFundingStreams()
        {
            return new OkObjectResult(Enumerable.Empty<FundingStream>());
        }

        [HttpGet("api/fundingstreams/{fundingStreamId}")]
        [Produces(typeof(FundingStream))]
        public async Task<IActionResult> GetFundingStreamById([FromRoute]string fundingStreamId)
        {
            return new OkObjectResult(new FundingStream());
        }

        [HttpPost("api/fundingstreams")]
        [Produces(typeof(FundingStream))]
        public async Task<IActionResult> SaveFundingStream([FromBody]string fundingStreamYaml)
        {
            return new OkObjectResult(new FundingStream());
        }
    }
}
