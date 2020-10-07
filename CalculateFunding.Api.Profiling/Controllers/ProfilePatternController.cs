using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Profiling.Controllers
{
    [Authorize(Roles = "ExecuteApi")]
    [ApiController]
    public class ProfilePatternController : ControllerBase
    {
        private readonly IProfilePatternService _profilePatternService;

        public ProfilePatternController(IProfilePatternService profilePatternService)
        {
            _profilePatternService = profilePatternService;
        }

        /// <summary>
        /// Get a profile pattern by ID
        /// </summary>
        /// <param name="id">Profile pattern ID.</param>
        /// <returns></returns>
        [HttpGet("api/profiling/patterns/{id}")]
        [Produces(typeof(FundingStreamPeriodProfilePattern))]
        public async Task<IActionResult> GetProfilePattern([FromRoute] string id)
        {
            return await _profilePatternService.GetProfilePattern(id);
        }

        /// <summary>
        /// Get profile patterns for funding stream and period
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <returns></returns>
        [HttpGet("api/profiling/patterns/fundingStream/{fundingStreamId}/fundingPeriod/{fundingPeriodId}")]
        [Produces(typeof(IEnumerable<FundingStreamPeriodProfilePattern>))]
        public async Task<IActionResult> GetProfilePatterns([FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId)
        {
            return await _profilePatternService.GetProfilePatterns(fundingStreamId, fundingPeriodId);
        }

        /// <summary>
        /// Create profile pattern.
        /// The profile pattern ID shouldn't already be in use within the funding stream and period
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("api/profiling/patterns")]
        [Produces(typeof(HttpStatusCode))]
        public async Task<IActionResult> CreateProfilePattern([FromBody] CreateProfilePatternRequest request)
        {
            return await _profilePatternService.CreateProfilePattern(request);
        }

        /// <summary>
        /// Modify or create and existing profile pattern.
        /// The pattern is in the context of a funding stream, funding period, funding line and profile pattern key
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("api/profiling/patterns")]
        [Produces(typeof(HttpStatusCode))]
        public async Task<IActionResult> UpsertProfilePattern([FromBody] UpsertProfilePatternRequest request)
        {
            return await _profilePatternService.UpsertProfilePattern(request);
        }


        /// <summary>
        /// Delete a profile pattern
        /// </summary>
        /// <param name="id">Profile pattern ID</param>
        /// <returns></returns>
        [HttpDelete("api/profiling/patterns/{id}")]
        [Produces(typeof(HttpStatusCode))]
        public async Task<IActionResult> DeleteProfilePattern([FromRoute] string id)
        {
            return await _profilePatternService.DeleteProfilePattern(id);
        }
    }
}