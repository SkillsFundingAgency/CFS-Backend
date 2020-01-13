using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.CosmosDbScaling.Controllers
{
    [ApiController]
    public class CosmosDbScalingController : ControllerBase
    {
        private readonly ICosmosDbScalingService _cosmosDbScalingService;

        public CosmosDbScalingController(
            ICosmosDbScalingService cosmosDbScalingService)
        {
            Guard.ArgumentNotNull(cosmosDbScalingService, nameof(_cosmosDbScalingService));

            _cosmosDbScalingService = cosmosDbScalingService;
        }

        [Route("api/cosmosdbscaling/scalingconfig")]
        [HttpPost]
        [Produces(typeof(ScalingConfigurationUpdateModel))]
        public async Task<IActionResult> RunSaveConfiguration([FromBody]ScalingConfigurationUpdateModel scalingConfigurationUpdate)
        {
            return await _cosmosDbScalingService.SaveConfiguration(scalingConfigurationUpdate);
        }
    }
}
