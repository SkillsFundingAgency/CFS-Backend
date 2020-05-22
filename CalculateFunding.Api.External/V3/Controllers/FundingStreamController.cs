using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/funding-streams")]
    public class FundingStreamController : ControllerBase
    {
        private readonly IFundingStreamService _fundingStreamService;

        public FundingStreamController(IFundingStreamService fundingStreamService)
        {
            _fundingStreamService = fundingStreamService;
        }

        [HttpGet()]
        [ProducesResponseType(200, Type = typeof(IEnumerable<FundingStream>))]
        public async Task<IActionResult> GetFundingStreams()
        {
            return await _fundingStreamService.GetFundingStreams();
        }

        [HttpGet("{fundingStreamId}/funding-periods")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<FundingPeriod>))]
        public async Task<IActionResult> GetFundingPeriods(string fundingStreamId)
        {
            return await _fundingStreamService.GetFundingPeriods(fundingStreamId);
        }
    }
}
