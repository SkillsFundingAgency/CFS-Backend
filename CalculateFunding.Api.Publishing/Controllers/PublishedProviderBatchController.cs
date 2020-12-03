using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishedProviderBatchController : ControllerBase
    {
        private readonly IBatchUploadService _batchUploadService;
        private readonly IBatchUploadValidationService _validationService;
        private readonly IBatchUploadQueryService _batchUploadQueryService;


        public PublishedProviderBatchController(IBatchUploadService batchUploadService,
            IBatchUploadValidationService validationService,
            IBatchUploadQueryService batchUploadQueryService)
        {
            Guard.ArgumentNotNull(batchUploadService, nameof(batchUploadService));
            Guard.ArgumentNotNull(validationService, nameof(validationService));
            Guard.ArgumentNotNull(batchUploadQueryService, nameof(batchUploadQueryService));
            
            _batchUploadService = batchUploadService;
            _validationService = validationService;
            _batchUploadQueryService = batchUploadQueryService;
        }

        [HttpPost("api/publishedproviderbatch")]
        public async Task<IActionResult> UploadBatch([FromBody] BatchUploadRequest batchUploadRequest)
            => await _batchUploadService.UploadBatch(batchUploadRequest);

        [HttpPost("api/publishedproviderbatch/validate")]
        public async Task<IActionResult> ValidateBatch([FromBody] BatchUploadValidationRequest validationRequest)
            => await _validationService.QueueBatchUploadValidation(validationRequest,
                Request.GetUser(),
                Request.GetCorrelationId());

        [HttpGet("api/publishedproviderbatch/{batchId}/publishedProviders")]
        public async Task<IActionResult> GetBatchPublishedProviderIds([FromRoute] string batchId)
            => await _batchUploadQueryService.GetBatchProviderIds(batchId);
    }
}