using Swashbuckle.AspNetCore.SwaggerGen;
using CalculateFunding.Api.External.ExampleProviders;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Models.External.AtomItems;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.Controllers
{
    [Route("api/allocations/notifications")]
    [Produces("application/vnd.sfa.allocation.1+atom+xml", "application/vnd.sfa.allocation.1+atom+json")]
    public class AllocationNotificationsController : Controller
    {
        /// <summary>
        /// Retrieves notifications of allocation events. These may be the creation, updating or publication of allocations.
        /// </summary>
        /// <param name="pageRef">Optional page number of notification results. Please see the links in the atom feed for available pages</param>
        /// <param name="allocationStatus">
        /// The allocation status you want to filter request by. Default is Approved
        /// ### Allocation Statuses available in the system
        ///
        ///| Status    | Visible to Api      | Visible to Provider | Description                                                                                                                                                                            |
        ///|-------    |----------------     |---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
        ///| Held      | No                  | No                        | This is the draft status of the allocation created initially when our specification is chosen to allocate funding for that time period. This status can also be set manually if for some reason they want to stop a previously approved/published allocation from being available|
        ///| Approved  | Yes                 | No                        | This status means the approver has reviewed the allocation internally using Calculate Funding Service and are happy that the result is accurate. There are business reasons they want to delay releasing this information to the provider. (i.e. wait for all the providers in a local authority to be approved)|
        ///| Updated   | Yes                 | No                        | This status means result of this calculation has been changed and new version of calculation is available.|
        ///| Published | Yes                 | Yes                       | This status indicates ESFA are happy for the funding (both money and information about that funding) are good to be made external to the provider.|
        /// </param>
        /// <returns></returns>
        [HttpGet("{pageRef}")]
        [Produces(typeof(AtomFeed))]
        [SwaggerResponseExample(200, typeof(AllocationNotificationExamples))]
        [SwaggerOperation("getAllocationNotifications")]
        [SwaggerOperationFilter(typeof(OperationFilter<AtomFeed>))]
        [ProducesResponseType(typeof(AtomFeed), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        [ProducesResponseType(500)]
        public IActionResult GetNotifications(int? pageRef = null, string allocationStatus = "Approved")
        {
            return Formatter.ActionResult<AllocationNotificationExamples, AtomFeed>(Request);
        }
    }
}
