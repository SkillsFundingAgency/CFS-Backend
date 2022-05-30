using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Helpers;
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
        private readonly IPublishedFundingCsvJobsService _publishFundingCsvJobsService;

        public FundingActionsController(
            ISpecificationPublishingService specificationPublishingService,
            IProviderFundingPublishingService providerFundingPublishingService,
            IPublishedFundingCsvJobsService publishFundingCsvJobsService)
        {
            Guard.ArgumentNotNull(specificationPublishingService, nameof(specificationPublishingService));
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));
            Guard.ArgumentNotNull(publishFundingCsvJobsService, nameof(publishFundingCsvJobsService));

            _specificationPublishingService = specificationPublishingService;
            _providerFundingPublishingService = providerFundingPublishingService;
            _publishFundingCsvJobsService = publishFundingCsvJobsService;
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

        /// <summary>
        /// Queue all csv jobs
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/queue-all-csv-jobs")]
        [ProducesResponseType(200, Type = typeof(Job))]
        public async Task<IActionResult> QueueAllCsvJobs([FromRoute] string specificationId)
        {
            (Job ParentJob, IEnumerable<Job> Child) approvalJobs = await _publishFundingCsvJobsService.QueueCsvPublishingJobs(GeneratePublishingCsvJobsCreationAction.Approve,
                specificationId,
                Request.GetCorrelationId(),
                Request.GetUser());

            await _publishFundingCsvJobsService.QueueCsvPublishingJobs(GeneratePublishingCsvJobsCreationAction.Refresh,
                specificationId,
                Request.GetCorrelationId(),
                Request.GetUser(),
                parentJob: approvalJobs.ParentJob);

            await _publishFundingCsvJobsService.QueueCsvPublishingJobs(GeneratePublishingCsvJobsCreationAction.Release,
                specificationId,
                Request.GetCorrelationId(),
                Request.GetUser(),
                parentJob: approvalJobs.ParentJob);

            return new OkObjectResult(approvalJobs.ParentJob);
        }

        private string GetCorrelationId()
        {
            return Request.GetCorrelationId();
        }
    }
}
