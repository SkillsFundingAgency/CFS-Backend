using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class UndoPublishingController : ControllerBase
    {
        private readonly IPublishedFundingUndoJobService _service;

        public UndoPublishingController(IPublishedFundingUndoJobService service)
        {
            Guard.ArgumentNotNull(service, nameof(service));
            
            _service = service;
        }
        
        [HttpGet("api/publishing/undo/{forCorrelationId}/{hardDelete}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(Job), 200)]
        public async Task<IActionResult> DeleteSpecification([FromRoute] string forCorrelationId, [FromRoute] bool hardDelete)
        {
            return Ok(await _service.QueueJob(forCorrelationId,
                hardDelete,
                Request.GetUser(),
                Request.GetCorrelationId()));
        }
    }
}