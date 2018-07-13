using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CalculateFunding.Models.External;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;

namespace CalculateFunding.Api.External.Controllers
{
    [ApiController]
    [Route("api/allocations")]
    public class AllocationsController : Controller
    {
        /// <summary>
        /// Return a given allocation. By default the latest published allocation is returned, or 404 if none is published. 
        /// An optional specific version can be requested
        /// </summary>
        /// <param name="allocationId">The id of the requested allocation</param>
        /// <param name="version">An optional version reference for a specific version</param>
        /// <param name="accept">The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format.</param>
        /// <param name="ifNoneMatch">If a previously provided ETag value is provided, the service will return a 304 Not Modified response as the resource has not changed.</param>
        [HttpGet("{allocationId}")]

        [ProducesResponseType(typeof(Allocation), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public ActionResult GetAllocation(
            string allocationId,
            [FromQuery(Name = "version")] int? version,
            [FromHeader(Name = "If-None-Match")] string ifNoneMatch,
            [Required,FromHeader(Name = "Accept")] string accept = "application/vnd.sfa.allocation.1+json")
        {
            IReadOnlyCollection<AllocationLine> allocationLines = new List<AllocationLine>()
            {
                new AllocationLine("a1", "alloc1"),
                new AllocationLine("a2", "alloc2"),
                new AllocationLine("a3", "alloc3")
            };

            FundingStream fundingStream1 =
                new FundingStream("FundingStreamCode1", "FundingStreamName1", allocationLines);

            Provider provider = new Provider("10025222", "A21345", DateTime.MinValue.ToShortDateString(), "Education and Skills Funding Agency");

            Period period1819 = new Period("AY", "AY1819", DateTime.MinValue.ToShortDateString(), DateTime.MaxValue.ToShortDateString());

            Allocation dummyAllocationToReturn = new Allocation(fundingStream1, period1819, provider, allocationLines.ElementAt(0), 1, "Status", 10000d, 200);

            return Ok(dummyAllocationToReturn);
        }
    }
}
