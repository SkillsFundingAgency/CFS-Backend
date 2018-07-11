using System.Collections.Generic;
using CalculateFunding.Models.External;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Internal;


namespace CalculateFunding.Api.External.Controllers
{
    [ApiController]
    [Route("funding-streams")]
    public class FundingStreamController : Controller
    {
        /// <summary>
        /// Return the funding streams supported by the service
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IList<FundingStream>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        
        public ActionResult GetFundingStreams()
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
