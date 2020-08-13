using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class FundingDateController : ControllerBase
    {
        private readonly IFundingDateService _fundingDateService;

        public FundingDateController(IFundingDateService fundingDateService)
        {
            _fundingDateService = fundingDateService;
        }

        [HttpGet("api/fundingdates/{fundingStreamId}/{fundingPeriodId}/{fundingLineId}")]
        [Produces(typeof(FundingDate))]
        public async Task<IActionResult> GetFundingDate(
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromRoute] string fundingLineId)
        {
            return await _fundingDateService.GetFundingDate(
                fundingStreamId, fundingPeriodId, fundingLineId);
        }

        [HttpPost("api/fundingdates/{fundingStreamId}/{fundingPeriodId}/{fundingLineId}")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SetFundingDates(
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromRoute] string fundingLineId,
            [FromBody] FundingDateViewModel fundingDateViewModel)
        {
                string controllerName =
                    ControllerContext.RouteData.Values.ContainsKey("controller") ?
                    (string)ControllerContext.RouteData.Values["controller"] :
                    string.Empty;

            return await _fundingDateService.SaveFundingDate(
                nameof(GetFundingDate),
                controllerName,
                fundingStreamId, 
                fundingPeriodId, 
                fundingLineId, 
                fundingDateViewModel);
        }
    }
}
