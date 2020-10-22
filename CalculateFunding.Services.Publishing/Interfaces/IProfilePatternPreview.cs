using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProfilePatternPreview
    {
        Task<IActionResult> PreviewProfilingChange(ProfilePreviewRequest request);
    }
}