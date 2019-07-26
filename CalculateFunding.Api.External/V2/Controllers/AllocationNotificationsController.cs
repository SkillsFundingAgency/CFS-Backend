using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Api.External.V2.Interfaces;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Api.External.V2.Models.Examples;
using CalculateFunding.Models.External.AtomItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.V2.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/allocations/notifications")]
    public class AllocationNotificationsController : Controller
    {
        private readonly IAllocationNotificationFeedsService _allocationFeedsService;

        public AllocationNotificationsController(IAllocationNotificationFeedsService allocationFeedsService)
        {
            _allocationFeedsService = allocationFeedsService;
        }

        ///  <summary>
        ///  Retrieves notifications of allocation events. These may be the creation, updating or publication of allocations.
        ///  </summary>
        ///  <param name="ukprn">Optional ukprns to filter by</param>
        ///  <param name="laCode">Optional lacodes to filter by</param>
        ///  <param name="isAllocationLineContractRequired">Optional allocationLineContractRequired to filter by</param>
        ///  <param name="pageSize">Optional page size (number of items returning for each page</param>
        ///  <param name="startYear">Optional funding period start year to filter by</param>
        ///  <param name="endYear">Optional funding period end year to filter by</param>
        ///  <param name="fundingStreamId">Optional funding stream Ids to filter by</param>
        ///  <param name="allocationLineId">Optional allocation line ids to filter by</param>
        ///  <param name="allocationStatus">
        ///  Optional filtering by statuses of notification results, by default this only returns **Published**. 
        ///  Send multiple query string parameters to bring back records of multiple statuses eg allocationStatuses=Published&amp;allocationStatuses=Approved
        /// Please see the table in the atom feed for available statuses
        ///  ### Allocation Statuses available in the system
        ///   
        ///   | Status    | Visible to Api      | Visible to Provider | Description                                                                                                                                                                            |
        ///   |-------    |----------------     |---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
        ///   | Approved  | Yes                 | No                        | This status means the approver has reviewed the allocation internally using Calculate Funding Service and are happy that the result is accurate. There are business reasons they want to delay releasing this information to the provider. (i.e. wait for all the providers in a local authority to be approved)|
        ///   | Updated   | Yes                 | No                        | This status means result of this calculation has been changed and new version of calculation is available.|
        ///   | Published | Yes                 | Yes                       | This status indicates ESFA are happy for the funding (both money and information about that funding) are good to be made external to the provider.|
        ///    </param>
        ///  <returns></returns>
        [HttpGet("")]
        [Produces(typeof(AtomFeed<AllocationModel>))]
        [SwaggerResponseExample(200, typeof(AllocationNotificationExamples))]
        [SwaggerOperation("getAllocationNotifications")]
        [SwaggerOperationFilter(typeof(OperationFilter<AtomFeed<AllocationModel>>))]
        [ProducesResponseType(typeof(AtomFeed<AllocationModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int? startYear,
            [FromQuery] int? endYear,
            [FromQuery] string[] allocationStatus,
            [FromQuery] string[] fundingStreamId,
            [FromQuery] string[] allocationLineId,
            [FromQuery] string[] ukprn,
            [FromQuery] string[] laCode,
            [FromQuery] bool? isAllocationLineContractRequired,
            [FromQuery] int? pageSize)
        {
            return await _allocationFeedsService.GetNotifications(Request, null, startYear, endYear, fundingStreamId,
                allocationLineId, allocationStatus, ukprn, laCode, isAllocationLineContractRequired, pageSize);
        }

        ///  <summary>
        ///  Retrieves notifications of allocation events. These may be the creation, updating or publication of allocations.
        ///  </summary>
        ///  <param name="ukprn">Optional ukprns to filter by</param>
        ///  <param name="laCode">Optional lacodes to filter by</param>
        ///  <param name="isAllocationLineContractRequired">Optional allocationLineContractRequired to filter by</param>
        ///  <param name="pageRef">Optional page number of notification results. Please see the links in the atom feed for available pages</param>
        ///  <param name="pageSize">Optional page size (number of items returning for each page</param>
        ///  <param name="startYear">Optional funding period start year to filter by</param>
        ///  <param name="endYear">Optional funding period end year to filter by</param>
        ///  <param name="fundingStreamId">Optional funding stream Ids to filter by</param>
        ///  <param name="allocationLineId">Optional allocation line ids to filter by</param>
        ///  <param name="allocationStatus">
        ///  Optional filtering by statuses of notification results, by default this only returns **Published**. 
        ///  Send multiple query string parameters to bring back records of multiple statuses eg allocationStatuses=Published&amp;allocationStatuses=Approved
        /// Please see the table in the atom feed for available statuses
        ///  ### Allocation Statuses available in the system
        ///   
        ///   | Status    | Visible to Api      | Visible to Provider | Description                                                                                                                                                                            |
        ///   |-------    |----------------     |---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
        ///   | Approved  | Yes                 | No                        | This status means the approver has reviewed the allocation internally using Calculate Funding Service and are happy that the result is accurate. There are business reasons they want to delay releasing this information to the provider. (i.e. wait for all the providers in a local authority to be approved)|
        ///   | Updated   | Yes                 | No                        | This status means result of this calculation has been changed and new version of calculation is available.|
        ///   | Published | Yes                 | Yes                       | This status indicates ESFA are happy for the funding (both money and information about that funding) are good to be made external to the provider.|
        ///    </param>
        ///  <returns></returns>
        [HttpGet("{pageRef:int}")]
        [Produces(typeof(AtomFeed<AllocationModel>))]
        [SwaggerResponseExample(200, typeof(AllocationNotificationExamples))]
        [SwaggerOperation("getAllocationNotifications")]
        [SwaggerOperationFilter(typeof(OperationFilter<AtomFeed<AllocationModel>>))]
        [ProducesResponseType(typeof(AtomFeed<AllocationModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> GetNotificationsByPageRef(
            [FromQuery] int? startYear,
            [FromQuery] int? endYear,
            [FromQuery] string[] allocationStatus,
            [FromQuery] string[] fundingStreamId,
            [FromQuery] string[] allocationLineId,
            [FromQuery] string[] ukprn,
            [FromQuery] string[] laCode,
            [FromQuery] bool? isAllocationLineContractRequired,
            [FromRoute] int pageRef,
            [FromQuery] int? pageSize)
        {
            return await _allocationFeedsService.GetNotifications(Request, pageRef, startYear, endYear, fundingStreamId,
                allocationLineId, allocationStatus, ukprn, laCode, isAllocationLineContractRequired, pageSize);
        }
    }
}