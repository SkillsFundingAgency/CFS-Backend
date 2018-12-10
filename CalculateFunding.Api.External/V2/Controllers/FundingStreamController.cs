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
        /// <returns></returns>
        [HttpGet]
        [Produces(typeof(FundingStream[]))]
        [SwaggerResponseExample(200, typeof(FundingStreamsExamples))]
        [SwaggerOperation("getFundingStreams")]
        [SwaggerOperationFilter(typeof(OperationFilter<FundingStream[]>))]
        [ProducesResponseType(typeof(IEnumerable<FundingStream>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFundingStreams()
        {
            return await _fundingStreamsService.GetFundingStreams();
        }

        /// <summary>
        /// Return the funding stream identified by the fundingStreamId parameter
        /// </summary>
        /// <param name="fundingStreamId">The id of the funding stream</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{fundingStreamId}")]
        [Produces(typeof(FundingStream))]
        [SwaggerResponseExample(200, typeof(FundingStreamExample))]
        [SwaggerOperation("getFundingStreamById")]
        [SwaggerOperationFilter(typeof(OperationFilter<FundingStream>))]
        [ProducesResponseType(typeof(FundingStream), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFundingStreamById(string fundingStreamId)
        {
            return await _fundingStreamsService.GetFundingStream(fundingStreamId);
        }
    }
}
