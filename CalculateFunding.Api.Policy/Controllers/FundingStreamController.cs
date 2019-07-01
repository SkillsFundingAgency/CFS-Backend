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

        /// <summary>
        /// Get all funding streams
        /// </summary>
        /// <returns>A list of funding streams</returns>
        [HttpGet("api/fundingstreams")]
        [Produces(typeof(IEnumerable<FundingStream>))]
        public async Task<IActionResult> GetFundingStreams()
        {
            return await _fundingStreamService.GetFundingStreams();
        }

        /// <summary>
        /// Gets a specific funding stream by ID
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID eg PES</param>
        /// <returns>Funding Stream</returns>
        [HttpGet("api/fundingstreams/{fundingStreamId}")]
        [Produces(typeof(FundingStream))]
        public async Task<IActionResult> GetFundingStreamById([FromRoute]string fundingStreamId)
        {
            return await _fundingStreamService.GetFundingStreamById(fundingStreamId);
        }


        /// <summary>
        /// Saves (creates or updates) a funding stream
        /// </summary>
        [HttpPost("api/fundingstreams")]
        [Produces(typeof(FundingStream))]
        public async Task<IActionResult> SaveFundingStream()
        {
            return await _fundingStreamService.SaveFundingStream(ControllerContext.HttpContext.Request);
        }
    }
}
