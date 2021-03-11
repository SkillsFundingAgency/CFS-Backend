using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Specs.Controllers
{
    [ApiController]
    public class SpecificationProviderController : ControllerBase
    {
        private readonly IQueueCreateSpecificationJobActions _queueCreateSpecificationJobActions;

        public SpecificationProviderController(IQueueCreateSpecificationJobActions queueCreateSpecificationJobActions)
        {
            _queueCreateSpecificationJobActions = queueCreateSpecificationJobActions;
        }

        [HttpPost("api/specs/createProviderSnapshotDataLoadJob")]
        public async Task<ActionResult<Job>> CreateProviderSnapshotDataLoadJob([FromBody] ProviderSnapshotDataLoadRequest request)
        {
            return await _queueCreateSpecificationJobActions.CreateProviderSnapshotDataLoadJob(request, Request.GetUser(), Request.GetCorrelationId());
        }
    }
}
