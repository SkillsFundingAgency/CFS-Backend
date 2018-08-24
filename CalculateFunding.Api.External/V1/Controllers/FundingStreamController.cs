using System.Collections.Generic;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Api.External.V1.Models.Examples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Api.External.V1.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [Route("api/funding-streams")]
    public class FundingStreamController : Controller
    {
        private readonly IFundingStreamService _fundingStreamsService;

        public FundingStreamController(IFundingStreamService fundingStreamService)
        {
            Guard.ArgumentNotNull(fundingStreamService, nameof(fundingStreamService));
            _fundingStreamsService = fundingStreamService;
        }
        /// <summary>
        /// Return the funding streams supported by the service
        /// </summary>
        /// <param name="ifNoneMatch">if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed.</param>
        /// <param name="Accept">The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format.</param>
        /// <returns></returns>
        [HttpGet]
        [Produces(typeof(IEnumerable<FundingStream>))]
        [SwaggerResponseExample(200, typeof(FundingStreamExamples))]
        [SwaggerOperation("getFundingStreams")]
        [SwaggerOperationFilter(typeof(OperationFilter<List<FundingStream>>))]
        [ProducesResponseType(typeof(IEnumerable<FundingStream>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [SwaggerResponseHeader(200, "ETag", "string", "An ETag of the resource")]
        [SwaggerResponseHeader(200, "Cache-Control", "string", "Caching information for the resource")]
        [SwaggerResponseHeader(200, "Last-Modified", "date", "Date the resource was last modified")]


        public async Task<IActionResult> GetFundingStreams()
        {
            return await _fundingStreamsService.GetFundingStreams(Request);
        }
    }
}
