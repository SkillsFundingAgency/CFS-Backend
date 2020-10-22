using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class FundingActionsController : ControllerBase
    {
        private readonly ISpecificationPublishingService _specificationPublishingService;
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;

        public FundingActionsController(
            ISpecificationPublishingService specificationPublishingService,
            IProviderFundingPublishingService providerFundingPublishingService)
        {
            Guard.ArgumentNotNull(specificationPublishingService, nameof(specificationPublishingService));
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));

            _specificationPublishingService = specificationPublishingService;
            _providerFundingPublishingService = providerFundingPublishingService;
        }

        /// <summary>
        /// Refresh funding for a specification
        /// </summary>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/refresh")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> RefreshFundingForSpecification([FromRoute] string specificationId)
        {
            return await _specificationPublishingService.CreateRefreshFundingJob(specificationId,
                Request.GetUser(),
                GetCorrelationId());
        }

        /// <summary>
        /// Validate Refresh funding for a specification
        /// </summary>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/validate-specification-for-refresh")]
        [ProducesResponseType(400, Type = typeof(IEnumerable<string>))]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ValidateSpecificationForRefresh
            ([FromRoute] string specificationId)
        {
            return await _specificationPublishingService.ValidateSpecificationForRefresh(specificationId);
        }

        /// <summary>
        /// Approve funding for a specification
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/approve")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> ApproveAllProviderFunding([FromRoute] string specificationId)
        {
            return await _specificationPublishingService.ApproveAllProviderFunding(
                specificationId,
                Request.GetUser(),
                GetCorrelationId());
        }

        /// <summary>
        /// Approve funding for batch providers within given specification
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <param name="publishedProviderIdsRequest"></param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/approve-providers")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> ApproveBatchProviderFunding(
            [FromRoute] string specificationId,
            [FromBody] PublishedProviderIdsRequest publishedProviderIdsRequest)
        {
            return await _specificationPublishingService.ApproveBatchProviderFunding(
                specificationId,
                publishedProviderIdsRequest,
                Request.GetUser(),
                GetCorrelationId());
        }

        /// <summary>
        /// Publish all provider funding
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/publish")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> PublishAllProviderFunding(
            [FromRoute] string specificationId)
        {
            return await _providerFundingPublishingService.PublishAllProvidersFunding(specificationId,
                Request.GetUser(),
                GetCorrelationId());
        }

        /// <summary>
        /// Publish funding for batch providers within given specification
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <param name="publishedProviderIdsRequest"></param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/publish-providers")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> PublishBatchProvidersFunding(
            [FromRoute] string specificationId,
            [FromBody] PublishedProviderIdsRequest publishedProviderIdsRequest)
        {
            return await _providerFundingPublishingService.PublishBatchProvidersFunding(
                specificationId,
                publishedProviderIdsRequest,
                Request.GetUser(),
                GetCorrelationId());
        }

        private string GetCorrelationId()
        {
            return Request.GetCorrelationId();
        }
    }
}
