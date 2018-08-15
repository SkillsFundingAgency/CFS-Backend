using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Api.External.V1.Models.Examples;
using CalculateFunding.Models.External.AtomItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.V1.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
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
        /// <param name="allocationStatuses">Optional comm a seperated list of statuses of notification results. Please see the links in the atom feed for available pages
        /// The allocation status you want to filter request by. Default is Published
        /// </param>
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