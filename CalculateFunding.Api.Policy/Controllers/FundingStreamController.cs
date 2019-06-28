using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class FundingStreamController : ControllerBase
    {
        private readonly IFundingStreamService _fundingStreamService;

        public FundingStreamController(IFundingStreamService fundingStreamService)
        {
            _fundingStreamService = fundingStreamService;
        }

        [HttpGet("api/fundingstreams")]
        [Produces(typeof(IEnumerable<FundingStream>))]
        public async Task<IActionResult> GetFundingStreams()
        {
            return await _fundingStreamService.GetFundingStreams();
        }

        [HttpGet("api/fundingstreams/{fundingStreamId}")]
        [Produces(typeof(FundingStream))]
        public async Task<IActionResult> GetFundingStreamById([FromRoute]string fundingStreamId)
        {
            return await _fundingStreamService.GetFundingStreamById(fundingStreamId);
        }

        [HttpPost("api/fundingstreams")]
        [Produces(typeof(FundingStream))]
        public async Task<IActionResult> SaveFundingStream()
        {
            return await _fundingStreamService.SaveFundingStream(ControllerContext.HttpContext.Request);
        }
    }
}
