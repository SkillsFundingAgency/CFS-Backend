using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiController]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/funding-streams")]
    public class FundingStreamController : ControllerBase
    {
        private readonly IFundingStreamService _fundingStreamService;

        public FundingStreamController(IFundingStreamService fundingStreamService)
        {
            _fundingStreamService = fundingStreamService;
        }

        [HttpGet]
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

        [HttpGet("{fundingStreamId}/funding-periods/{fundingPeriodId}/templates/{majorVersion}.{minorVersion}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFundingTemplateSourceFile(
            string fundingStreamId, string fundingPeriodId, int majorVersion, int minorVersion)
        {
            return await _fundingStreamService.GetFundingTemplateSourceFile(
                fundingStreamId,
                fundingPeriodId,
                majorVersion,
                minorVersion);
        }

        [HttpGet("{fundingStreamId}/funding-periods/{fundingPeriodId}/templates")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PublishedFundingTemplate>))]
        public async Task<IActionResult> GetPublishedFundingTemplates(string fundingStreamId, string fundingPeriodId)
        {
            return await _fundingStreamService.GetPublishedFundingTemplates(fundingStreamId,fundingPeriodId);
        }
    }
}
