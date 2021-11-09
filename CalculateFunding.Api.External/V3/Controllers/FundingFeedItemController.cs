using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V3.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiController]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/funding")]
    public class FundingFeedItemController : ControllerBase
    {
        private readonly IFundingFeedItemByIdService _fundingService;

        public FundingFeedItemController(IFundingFeedItemByIdService fundingService)
        {
            Guard.ArgumentNotNull(fundingService, nameof(fundingService));

            _fundingService = fundingService;
        }

        /// <summary>
        /// Return a given funding. By default the latest published funding is returned, or 404 if none is published. 
        /// An optional specific version can be requested
        /// </summary>
        /// <param name="id">The published funding id</param>
        [HttpGet("byId/{id}")]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Produces("application/json")]
        public async Task<IActionResult> GetFunding(string id)
        {
            return await _fundingService.GetFundingByFundingResultId(id);
        }
    }
}
