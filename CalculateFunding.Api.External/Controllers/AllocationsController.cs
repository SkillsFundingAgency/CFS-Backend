using CalculateFunding.Api.External.ExampleProviders;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.AtomItems;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Controllers
{
    //[ApiController]
    [Route("api/allocations")]
    //[Produces("application/vnd.sfa.allocation.1+json")]
    public class AllocationsController : Controller
    {
        /// <summary>
        /// Return a given allocation. By default the latest published allocation is returned, or 404 if none is published. 
        /// An optional specific version can be requested
        /// </summary>
        /// <param name="allocationId">The id of the requested allocation</param>
        /// <param name="version">An optional version reference for a specific version</param>
        [HttpGet("{allocationId}")]
        [Produces(typeof(Allocation))]
        [SwaggerResponseExample(200, typeof(AllocationExamples))]
        [SwaggerOperation("getAllocationById")]
        [SwaggerOperationFilter(typeof(OperationFilter<Allocation>))]
        [ProducesResponseType(typeof(Allocation), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [SwaggerResponseHeader(200, "ETag", "string", "An ETag of the resource")]
        [SwaggerResponseHeader(200, "Cache-Control", "string", "Caching information for the resource")]
        [SwaggerResponseHeader(200, "Last-Modified", "date", "Date the resource was last modified")]
        public IActionResult GetAllocation(string allocationId, ushort? version = null)
        {
            return Formatter.ActionResult<AllocationExamples, Allocation>(Request);
        }
    }
}
