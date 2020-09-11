using System;
using System.Threading.Tasks;
using CacheCow.Server.Core.Mvc;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class FundingStructureController : ControllerBase
    {
        private readonly IFundingStructureService _fundingStructureService;

        public FundingStructureController(IFundingStructureService fundingStructureService)
        {
            Guard.ArgumentNotNull(fundingStructureService, nameof(fundingStructureService));
            
            _fundingStructureService = fundingStructureService;
        }

        [HttpPost("api/funding-structures/lastModified")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DateTimeOffset))]
        public async Task<IActionResult> UpdateFundingStructureLastModified([FromBody] UpdateFundingStructureLastModifiedRequest request) 
            => await _fundingStructureService.UpdateFundingStructureLastModified(request);

        [HttpGet("api/funding-structures")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FundingStructure))]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [HttpCacheFactory(0, ViewModelType = typeof(FundingStructure))]
        public async Task<IActionResult> GetFundingStructure([FromQuery] string fundingStreamId,
            [FromQuery] string fundingPeriodId,
            [FromQuery] string specificationId)
            => await _fundingStructureService.GetFundingStructure(fundingStreamId, fundingPeriodId, specificationId);
        
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