using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Api.External.V2.Interfaces;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Api.External.V2.Models.Examples;
using CalculateFunding.Services.Core.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.V2.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/funding-streams")]
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
        [Produces(typeof(FundingStream[]))]
        [SwaggerResponseExample(200, typeof(FundingStreamExamples))]
        [SwaggerOperation("getFundingStreams")]
        [SwaggerOperationFilter(typeof(OperationFilter<FundingStream[]>))]
        [ProducesResponseType(typeof(IEnumerable<FundingStream>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(406)]
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
