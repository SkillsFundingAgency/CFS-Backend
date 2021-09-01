using CalculateFunding.Models.Publishing.FundingManagement;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class ReleaseManagementActionsController : ControllerBase
    {
        public ReleaseManagementActionsController()
        {

        }

        [HttpPost("api/specifications/{specificationId}/releaseProvidersToChannels")]
        public async Task<ActionResult<string>> ReleaseProviderVersions(
            [FromRoute] string specificationId,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest)
        {
            // TODO: convert to job
            //await _svc.QueueReleaseProviderToChannelsJob(specificationId, releaseProvidersToChannelRequest, Request.GetUser(), Guid.NewGuid().ToString());

            return NoContent();
        }
    }
}
