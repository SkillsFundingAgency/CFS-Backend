using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.FundingPolicy.ViewModels;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class FundingConfigurationController : ControllerBase
    {
        private readonly IFundingConfigurationService _fundingConfigurationService;

        public FundingConfigurationController(IFundingConfigurationService fundingConfigurationService)
        {
            Guard.ArgumentNotNull(fundingConfigurationService, nameof(fundingConfigurationService));

            _fundingConfigurationService = fundingConfigurationService;
        }

        [HttpGet("api/configuration/{fundingStreamId}/{fundingPeriodId}")]
        [Produces(typeof(FundingConfiguration))]
        public async Task<IActionResult> GetFundingConfiguration([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId)
        {
            return await _fundingConfigurationService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);
        }



        [HttpPost("api/configuration/{fundingStreamId}/{fundingPeriodId}")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SaveFundingConfiguration([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId, [FromBody]FundingConfigurationViewModel configurationViewModel)
        {
            string controllerName = string.Empty;

            if (this.ControllerContext.RouteData.Values.ContainsKey("controller"))
            {
                controllerName = (string)this.ControllerContext.RouteData.Values["controller"];
            }

            return await _fundingConfigurationService.SaveFundingConfiguration(
                nameof(GetFundingConfiguration),
                controllerName,
                configurationViewModel,
                fundingStreamId,
                fundingPeriodId);
        }
    }
}
