using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Common.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("4.0")]
    [Route("api/v{version:apiVersion}/{channel}/funding")]
    public class FundingFeedItemControllerV4 : ControllerBase
    {
        private readonly IFundingFeedItemByIdService _fundingService;

        public FundingFeedItemControllerV4(IFundingFeedItemByIdService fundingService)
        {
            Guard.ArgumentNotNull(fundingService, nameof(fundingService));

            _fundingService = fundingService;
        }

        /// <summary>
        /// Return a given funding. By default the latest published funding is returned, or 404 if none is published. 
        /// An optional specific version can be requested
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="fundingId">The published funding group id</param>
        [HttpGet("byId/{fundingId}")]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Produces("application/json")]
        public async Task<IActionResult> GetFunding([FromRoute] string channel, [FromRoute] string fundingId)
        {
            return await _fundingService.GetFundingByFundingResultId(channel, fundingId);
        }
    }
}
