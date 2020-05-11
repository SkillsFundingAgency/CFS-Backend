using CalculateFunding.Models.Calcs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IPreviewService
    {
        Task<IActionResult> Compile(PreviewRequest previewRequest);
    }
}
