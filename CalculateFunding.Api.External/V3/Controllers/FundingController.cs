using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Common.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.V3.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/funding")]
    public class FundingController : Controller
    {
        private readonly IFundingService _fundingService;

        public FundingController(IFundingService fundingService)
        {
            Guard.ArgumentNotNull(fundingService, nameof(fundingService));
            _fundingService = fundingService;
        }

        /// <summary>
        /// Return a given funding. By default the latest published funding is returned, or 404 if none is published. 
        /// An optional specific version can be requested
        /// </summary>
        /// <param name="id">The published funding id</param>
        [HttpGet("byId/{id}")]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Produces("application/json")]
        public async Task<IActionResult> GetFunding(string id)
        {
            return await _fundingService.GetFundingByFundingResultId(id);
        }
    }
}
