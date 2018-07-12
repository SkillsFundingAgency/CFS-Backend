using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CalculateFunding.Models.External;
using Microsoft.AspNetCore.Mvc;


namespace CalculateFunding.Api.External.Controllers
{
    [ApiController]
    [Route("api/funding-streams")]
    public class FundingStreamController : Controller
    {
        /// <summary>
        /// Return the funding streams supported by the service
        /// </summary>
        /// <param name="ifNoneMatch">if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed.</param>
        /// <param name="Accept">The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format.</param>
        /// <returns></returns>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IList<FundingStream>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        
        public ActionResult GetFundingStreams(
            [FromHeader(Name = "If-None-Match")]string ifNoneMatch,
            [Required,FromHeader(Name = "Accept")] string Accept = "application/vnd.sfa.allocation.1+json")
        {
            IReadOnlyCollection<AllocationLine> allocationLines = new List<AllocationLine>()
            {
                new AllocationLine("a1", "alloc1"),
                new AllocationLine("a2", "alloc2"),
                new AllocationLine("a3", "alloc3")
            };

            FundingStream fundingStream1 =
                new FundingStream("FundingStreamCode1", "FundingStreamName1", allocationLines);
            FundingStream fundingStream2 =
                new FundingStream("FundingStreamCode2", "FundingStreamName2", allocationLines);

            IList<FundingStream> fundingStreams = new List<FundingStream>() {fundingStream1, fundingStream2};
            return Ok(fundingStreams);
        }

        
    }
}
