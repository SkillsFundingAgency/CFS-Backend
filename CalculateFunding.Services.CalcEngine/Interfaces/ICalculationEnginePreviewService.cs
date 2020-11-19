using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ICalculationEnginePreviewService
    {
        Task<IActionResult> PreviewCalculationResult(
            string specificationId, 
            string providerId, 
            byte[] assemblyContent);
    }
}
