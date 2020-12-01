using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.CalcEngine.Controllers
{
    [ApiController]
    public class CalculationResultsController : ControllerBase
    {
        private readonly ICalculationEnginePreviewService _calculationEnginePreviewService;

        public CalculationResultsController(ICalculationEnginePreviewService calculationEnginePreviewService)
        {
            _calculationEnginePreviewService = calculationEnginePreviewService;
        }

        [Route("api/calculations-results/{specificationId}/{providerId}/preview")]
        [HttpPost]
        [Produces(typeof(ProviderResult))]
        public async Task<IActionResult> PreviewCalculationResults(
            [FromRoute] string specificationId,
            [FromRoute] string providerId,
            [FromBody] PreviewCalculationRequest previewCalculationRequest) =>
                await _calculationEnginePreviewService.PreviewCalculationResult(specificationId, providerId, previewCalculationRequest);
    }
}
