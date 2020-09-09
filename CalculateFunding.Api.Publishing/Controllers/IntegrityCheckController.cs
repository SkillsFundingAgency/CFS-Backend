using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class IntegrityCheckController : ControllerBase
    {
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;

        public IntegrityCheckController(
            IProviderFundingPublishingService providerFundingPublishingService
            )
        {
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));

            _providerFundingPublishingService = providerFundingPublishingService;
        }

        /// <summary>
        /// Publish integrity check
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/publishintegritycheck")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> PublishIntegrityCheck([FromRoute] string specificationId)
        {
            return await _providerFundingPublishingService.PublishIntegrityCheck(specificationId,
                GetUser(),
                GetCorrelationId());
        }

        /// <summary>
        /// Publish integrity check
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <param name="publishAll">Whether to publish all funding contents</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/publishintegritycheck/{publishAll}")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> PublishIntegrityCheck([FromRoute] string specificationId, [FromRoute] bool publishAll)
        {
            return await _providerFundingPublishingService.PublishIntegrityCheck(specificationId,
                GetUser(),
                GetCorrelationId(),
                publishAll);
        }

        private Reference GetUser()
        {
            return Request.GetUser();
        }

        private string GetCorrelationId()
        {
            return Request.GetCorrelationId();
        }
    }
}
