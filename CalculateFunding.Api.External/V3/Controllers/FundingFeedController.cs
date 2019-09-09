using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External.AtomItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V3.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/funding/notifications")]
    public class FundingFeedController : Controller
    {
        private readonly IFundingFeedService _fundingFeedsService;

        public FundingFeedController(IFundingFeedService fundingFeedsService)
        {
            Guard.ArgumentNotNull(fundingFeedsService, nameof(fundingFeedsService));
            _fundingFeedsService = fundingFeedsService;
        }

        /// <summary>
        /// Funding feed - initial page with latest results
        /// </summary>
        /// <param name="fundingStreamIds">Optional Funding stream IDs</param>
        /// <param name="fundingPeriodIds">Optional Funding Period IDs</param>
        /// <param name="groupingReasons">Optional Grouping Reasons</param>
        /// <param name="pageSize">Page Size</param>
        /// <returns>Feed of funding notifications based on query parameters</returns>
        [HttpGet("")]
        [Produces(typeof(AtomFeed<object>))]
        public async Task<IActionResult> GetFunding(
            [FromQuery] string[] fundingStreamIds,
            [FromQuery] string[] fundingPeriodIds,
            [FromQuery] GroupingReason[] groupingReasons,
            [FromQuery] int? pageSize)
        {
            return await _fundingFeedsService.GetFunding(Request, null, fundingStreamIds,
                 fundingPeriodIds, groupingReasons, pageSize);
        }


        /// <summary>
        /// Funding feed - specific historical page of results
        /// </summary>
        /// <param name="fundingStreamIds">Optional Funding stream IDs</param>
        /// <param name="fundingPeriodIds">Optional Funding Period IDs</param>
        /// <param name="groupingReasons">Optional Grouping Reasons</param>
        /// <param name="pageSize">Page Size</param>
        /// <param name="pageRef">Page reference for historical page</param>
        /// <returns></returns>
        [HttpGet("{pageRef:int}")]
        [Produces(typeof(AtomFeed<object>))]
        public async Task<IActionResult> GetFundingPage(
            [FromQuery] string[] fundingStreamIds,
            [FromQuery] string[] fundingPeriodIds,
            [FromQuery] GroupingReason[] groupingReasons,
            [FromQuery] int? pageSize,
            [FromRoute] int pageRef)
        {
            return await _fundingFeedsService.GetFunding(Request, pageRef, fundingStreamIds,
                 fundingPeriodIds, groupingReasons, pageSize);
        }
    }
}
