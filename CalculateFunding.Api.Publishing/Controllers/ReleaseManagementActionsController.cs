using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing.FundingManagement;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class ReleaseManagementActionsController : ControllerBase
    {
        private readonly IReleaseProvidersToChannelsService _releaseProvidersToChannelsService;

        public ReleaseManagementActionsController(
            IReleaseProvidersToChannelsService releaseProvidersToChannelsService)
        {
            _releaseProvidersToChannelsService = releaseProvidersToChannelsService;
        }

        [HttpPost("api/specifications/{specificationId}/releaseProvidersToChannels")]
        public async Task<IActionResult> QueueReleaseProviderVersions(
            [FromRoute] string specificationId,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return await _releaseProvidersToChannelsService.QueueReleaseProviderVersions(specificationId, releaseProvidersToChannelRequest, user, correlationId);
        }
    }
}
