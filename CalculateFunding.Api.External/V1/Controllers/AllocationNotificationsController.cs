using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Api.External.V1.Models.Examples;
using CalculateFunding.Models.External.AtomItems;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.V1.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/allocations/notifications")]
    public class AllocationNotificationsController : Controller
    {
        private readonly IAllocationNotificationFeedsService _allocationFeedsService;

        public AllocationNotificationsController(IAllocationNotificationFeedsService allocationFeedsService)
        {
            _allocationFeedsService = allocationFeedsService;
        }

        /// <summary>
        /// Retrieves notifications of allocation events. These may be the creation, updating or publication of allocations.
        /// </summary>
        /// <param name="pageRef">Optional page number of notification results. Please see the links in the atom feed for available pages</param>
        /// <param name="allocationStatuses">Optional comm a seperated list of statuses of notification results. Please see the links in the atom feed for available pages</param>
        /// ### Allocation Statuses available in the system
        ///
        ///| Status    | Visible to Api      | Visible to Provider | Description                                                                                                                                                                            |
        ///|-------    |----------------     |---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
        ///| Held      | No                  | No                        | This is the draft status of the allocation created initially when our specification is chosen to allocate funding for that time period. This status can also be set manually if for some reason they want to stop a previously approved/published allocation from being available|
        ///| Approved  | Yes                 | No                        | This status means the approver has reviewed the allocation internally using Calculate Funding Service and are happy that the result is accurate. There are business reasons they want to delay releasing this information to the provider. (i.e. wait for all the providers in a local authority to be approved)|
        ///| Updated   | Yes                 | No                        | This status means result of this calculation has been changed and new version of calculation is available.|
        ///| Published | Yes                 | Yes                       | This status indicates ESFA are happy for the funding (both money and information about that funding) are good to be made external to the provider.|
        /// <returns></returns>
        [HttpGet("{pageRef?}/{allocationStatuses?}")]
        [Produces(typeof(AtomFeed<AllocationModel>))]
        [SwaggerResponseExample(200, typeof(AllocationNotificationExamples))]
        [SwaggerOperation("getAllocationNotifications")]
        [SwaggerOperationFilter(typeof(OperationFilter<AtomFeed<AllocationModel>>))]
        [ProducesResponseType(typeof(AtomFeed<AllocationModel>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        [ProducesResponseType(503)]
        public Task<IActionResult> GetNotifications(int? pageRef = null, string allocationStatuses = "")
        {
            return _allocationFeedsService.GetNotifications(pageRef.Value, allocationStatuses, Request);
        }
    }
}