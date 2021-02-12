using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class ProviderProfilePreviewController : ControllerBase
    {
        private readonly IProfilePatternPreview _profilePatternPreview;

        public ProviderProfilePreviewController(IProfilePatternPreview profilePatternPreview)
        {
            Guard.ArgumentNotNull(profilePatternPreview, nameof(profilePatternPreview));

            _profilePatternPreview = profilePatternPreview;
        }

        [HttpPost("api/publishedproviderfundinglinepreview")]
        public async Task<IActionResult> PreviewProfileChange([FromBody] ProfilePreviewRequest request)
            => await _profilePatternPreview.PreviewProfilingChange(request);
    }
}