using System;
using System.Threading.Tasks;
using CacheCow.Server.Core.Mvc;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Result;
using CalculateFunding.Services.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Results.Controllers
{
    [ApiController]
    [Obsolete]
    public class FundingStructureController : ControllerBase
    {
        private readonly IFundingStructureService _fundingStructureService;

        public FundingStructureController(
            IFundingStructureService fundingStructureService)
        {
            Guard.ArgumentNotNull(fundingStructureService, nameof(fundingStructureService));

            _fundingStructureService = fundingStructureService;
        }

        [HttpGet("api/funding-structures/results")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FundingStructure))]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [HttpCacheFactory(0, ViewModelType = typeof(FundingStructure))]
        public async Task<IActionResult> GetFundingStructureWithCalculationResults([FromQuery] string fundingStreamId,
            [FromQuery] string fundingPeriodId,
            [FromQuery] string specificationId,
            [FromQuery] string providerId)
            => await _fundingStructureService.GetFundingStructureWithCalculationResults(fundingStreamId, fundingPeriodId, specificationId, providerId);
    }
}
