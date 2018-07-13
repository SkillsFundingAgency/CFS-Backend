using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CalculateFunding.Models.External;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CalculateFunding.Api.External.Controllers
{
    [ApiController]
    [Route("api/allocations/notifications")]
    public class AllocationNotificationsController : Controller
    {
        /// <summary>
        /// Retrieves notifications of allocation events. These may be the creation, updating or publication of allocations.
        /// </summary>
        /// <param name="pageRef">Optional page number of notification results. Please see the links in the atom feed for available pages</param>
        /// <param name="ifNoneMatch">if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed.</param>
        /// <param name="accept">The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format.</param>
        /// <returns></returns>
        [HttpGet("{pageRef}")]
        [ProducesResponseType(typeof(AtomFeed),200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        public ActionResult GetNotifications(
            int? pageRef, 
            [FromHeader(Name = "If-None-Match")] string ifNoneMatch, 
            [Required, FromHeader(Name = "Accept")] string accept = "application/vnd.sfa.allocation.1+json")
        {
            AtomLink atomLink1 = new AtomLink("https://api.calculate-funding.education.gov.uk/v1/finance?page=21", "self");
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


            AtomContent atomContent = new AtomContent(dummyAllocationToReturn, "type");
            AtomEntry atomEntry = new AtomEntry(Guid.NewGuid().ToString(),
                "Allocation Pupil Led Factors was Approved",
                "{URPRN: 1000063432, version: 3}",
                "2018-07-11T08:37:40.8973373+00:00",
                "1",
                atomLink1,
                atomContent
            );
            AtomAuthor feedAuthor = new AtomAuthor(null, "Calculate Funding Service");
            AtomFeed feedItem = new AtomFeed(
                Guid.NewGuid().ToString(), 
                "Allocation Pupil Led Factors was Approved", 
                feedAuthor, 
                "2018-07-12T08:37:40.8973176+00:00", 
                "Copyright (C) 2018 Department for Education",
                atomLink1,
                atomEntry
            );

            return Ok(feedItem);
        }
    }
}
