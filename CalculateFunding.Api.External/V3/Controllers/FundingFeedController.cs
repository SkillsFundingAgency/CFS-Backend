using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Models.External.AtomItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V3.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/funding/notifications")]
    public class FundingFeedController : ControllerBase
    {
        /// <summary>
        /// Funding feed - initial page with latest results
        /// </summary>
        /// <param name="fundingStreamId">Optional Funding stream IDs</param>
        /// <param name="fundingPeriodId">Optional Funding Period IDs</param>
        /// <param name="groupingReason">Optional Grouping Reasons</param>
        /// <param name="pageSize">Page Size</param>
        /// <returns>Feed of funding notifications based on query parameters</returns>
        [HttpGet("")]
        [Produces(typeof(AtomFeed<object>))]
        public async Task<IActionResult> GetFunding(
            [FromQuery] string[] fundingStreamId,
            [FromQuery] string[] fundingPeriodId,
            [FromQuery] GroupingReason[] groupingReason,
            [FromQuery] int? pageSize)
        {
            AtomFeed<object> result = new AtomFeed<object>();

            // Query search (PublishedFundingIndex) based on the parameters passed in to filter. Order by statusChangedDate DESC

            // Once you get the results back from search, retrieve all of the matching documents from blob storage and output as the feed item contents
            // Blob storage:
            // Account - cfs
            // Container - publishedfunding
            // Document path - documentPath/id.json

            // Ensure ATOM links work properly as per V2

            return await Task.FromResult(new OkObjectResult(result));
        }


        /// <summary>
        /// Funding feed - specific historical page of results
        /// </summary>
        /// <param name="fundingStreamId">Optional Funding stream IDs</param>
        /// <param name="fundingPeriodId">Optional Funding Period IDs</param>
        /// <param name="groupingReason">Optional Grouping Reasons</param>
        /// <param name="pageSize">Page Size</param>
        /// <param name="pageRef">Page reference for historical page</param>
        /// <returns></returns>
        [HttpGet("{pageRef:int}")]
        [Produces(typeof(AtomFeed<object>))]
        public async Task<IActionResult> GetFundingPage(
            [FromQuery] string[] fundingStreamId,
            [FromQuery] string[] fundingPeriodId,
            [FromQuery] GroupingReason[] groupingReason,
            [FromQuery] int? pageSize,
            [FromRoute] int pageRef)
        {
            AtomFeed<object> result = new AtomFeed<object>();

            // Query search (PublishedFundingIndex) based on the parameters passed in to filter. Order by statusChangedDate DESC, skip and take in search to get the correct page

            // Once you get the results back, retrieve all of the matching documents from blob storage and output as the feed item contents


            // Ensure ATOM links work properly as per V2

            return await Task.FromResult(new OkObjectResult(result));
        }
    }
}
