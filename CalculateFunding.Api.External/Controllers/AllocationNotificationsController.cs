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
        /// <returns></returns>
        [HttpGet]
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
        public IActionResult GetNotifications(int? pageRef = null)
        {
            return ExampleFormatter.ActionResult<AllocationNotificationExamples, AtomFeed>(Request);
        }
    }
}
