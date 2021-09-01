using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.AtomItems;
using CalculateFunding.Models.External.V4;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("4.0")]
    [Route("api/v{version:apiVersion}/{channel}/funding/notifications")]
    public class FundingFeedControllerV4 : ControllerBase
    {
        private readonly IFundingFeedServiceV4 _fundingFeedsService;

        public FundingFeedControllerV4(IFundingFeedServiceV4 fundingFeedsService)
        {
            Guard.ArgumentNotNull(fundingFeedsService, nameof(fundingFeedsService));
            _fundingFeedsService = fundingFeedsService;
        }

        /// <summary>
        /// Funding feed - initial page with latest results
        /// </summary>
        /// <param name="channel">Release purpose channel</param>
        /// <param name="fundingStreamIds">Optional Funding stream IDs</param>
        /// <param name="fundingPeriodIds">Optional Funding Period IDs</param>
        /// <param name="groupingReasons">Optional Grouping Reasons</param>
        /// <param name="variationReasons">Optional Variation Reasons</param>
        /// <param name="pageSize">Page Size</param>
        /// <returns>Feed of funding notifications based on query parameters</returns>
        [HttpGet("")]
        [Produces(typeof(AtomFeed<object>))]
        public async Task<ActionResult<SearchFeedResult<ExternalFeedFundingGroupItem>>> GetFunding(
            [FromRoute] string channel,
            [FromQuery] string[] fundingStreamIds,
            [FromQuery] string[] fundingPeriodIds,
            [FromQuery] GroupingReason[] groupingReasons,
            [FromQuery] VariationReason[] variationReasons,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken
            )
        {
            return await _fundingFeedsService.GetFundingNotificationFeedPage(Request, Response, null, channel, fundingStreamIds,
                 fundingPeriodIds, groupingReasons, variationReasons, pageSize, cancellationToken);
        }


        /// <summary>
        /// Funding feed - specific historical page of results.
        /// When requested with a specific page size, the items returned in the page will be the same.
        /// Page numbers with lower values container older items.
        /// </summary>
        /// <param name="channel">Release purpose channel</param>
        /// <param name="fundingStreamIds">Optional Funding stream IDs</param>
        /// <param name="fundingPeriodIds">Optional Funding Period IDs</param>
        /// <param name="groupingReasons">Optional Grouping Reasons</param>
        /// <param name="variationReasons">Optional Variation Reasons</param>
        /// <param name="pageSize">Page Size. Maximum size of 500</param>
        /// <param name="pageRef">Page reference for historical page</param>
        /// <returns></returns>
        [HttpGet("{pageRef:int}")]
        [Produces(typeof(AtomFeed<object>))]
        public async Task<ActionResult<SearchFeedResult<ExternalFeedFundingGroupItem>>> GetFundingPage(
            [FromRoute] string channel,
            [FromQuery] string[] fundingStreamIds,
            [FromQuery] string[] fundingPeriodIds,
            [FromQuery] GroupingReason[] groupingReasons,
            [FromQuery] VariationReason[] variationReasons,
            [FromQuery] int? pageSize,
            [FromRoute] int pageRef,
            CancellationToken cancellationToken)
        {
            return await _fundingFeedsService.GetFundingNotificationFeedPage(Request, Response, pageRef, channel, fundingStreamIds,
                 fundingPeriodIds, groupingReasons, variationReasons, pageSize, cancellationToken);
        }
    }
}
