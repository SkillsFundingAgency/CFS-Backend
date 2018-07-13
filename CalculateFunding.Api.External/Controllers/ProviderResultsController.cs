using System.ComponentModel.DataAnnotations;
using CalculateFunding.Models.External;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.Controllers
{
    [ApiController]
    [Route("api/providers/{ukprn}/periods/{periodId}")]
    public class ProviderResultsController : Controller
    {
        /// <summary>
        /// Returns a summary of funding stream totals for a given provider in a given period
        /// </summary>
        /// <param name="ukprn">The UKPRN identifying the provider</param>
        /// <param name="periodId">The required period</param>
        /// <param name="includeUnpublishedBy">default only published results are included. If includeUnpublished is true then the latest values are provided regardless of status</param>
        /// <param name="accept">The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format.</param>
        /// <param name="ifNoneMatch">if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed.</param>
        [HttpGet]
        [Route("summary")]
        [ProducesResponseType(typeof(ProviderResultSummary), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        public ActionResult Summary(
            string ukprn,
            string periodId,
            bool? includeUnpublishedBy,
            [FromHeader(Name = "If-None-Match")] string ifNoneMatch,
            [Required, FromHeader(Name = "Accept")] string accept = "application/vnd.sfa.allocation.1+json")
        {
            return Ok();
        }

        /// <summary>
        /// Returns a summary of funding stream totals for a given provider in a given period
        /// </summary>
        /// <param name="policyId">The required function stream’s code</param>
        /// <param name="ukprn">The UKPRN identifying the provider</param>
        /// <param name="periodId">The required period</param>
        /// <param name="accept">The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format.</param>
        /// <param name="includeUnpublishedBy">default only published results are included. If includeUnpublished is true then the latest values are provided regardless of status</param>
        /// <param name="ifNoneMatch">if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed.</param>
        [HttpGet]
        [Route("policies/{policyId}")]
        [ProducesResponseType(typeof(ProviderPolicyResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        public ActionResult Policies(
            string policyId,
            string ukprn,
            string periodId,
            bool? includeUnpublishedBy,
            [FromHeader(Name = "If-None-Match")] string ifNoneMatch,
            [Required, FromHeader(Name = "Accept")] string accept = "application/vnd.sfa.allocation.1+json")
        {
            return Ok();
        }

        /// <summary>
        /// Returns a summary of funding stream totals for a given provider in a given period
        /// </summary>
        /// <param name="ukprn">The UKPRN identifying the provider</param>
        /// <param name="periodId">The required period</param>
        /// <param name="fundingStreamCode">The required function stream’s code</param>
        /// <param name="accept">The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format.</param>
        /// <param name="includeUnpublishedBy">default only published results are included. If includeUnpublished is true then the latest values are provided regardless of status</param>
        /// <param name="ifNoneMatch">if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed.</param>
        /// <param name="allocationLineCode"></param>
        [HttpGet]
        [Route("funding-streams/{fundingStreamCode}/{allocationLineCode}")]
        [ProducesResponseType(typeof(ProviderFundingStreamResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        public ActionResult AllocationsLineCodes(
            string ukprn,
            string periodId,
            string fundingStreamCode,
            string allocationLineCode,
            bool? includeUnpublishedBy,
            [FromHeader(Name = "If-None-Match")] string ifNoneMatch,
            [Required, FromHeader(Name = "Accept")] string accept = "application/vnd.sfa.allocation.1+json")
        {
            return Ok();
        }
    }
}
