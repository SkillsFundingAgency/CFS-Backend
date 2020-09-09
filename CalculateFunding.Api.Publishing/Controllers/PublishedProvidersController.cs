using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishedProvidersController : ControllerBase
    {
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;

        public PublishedProvidersController(IProviderFundingPublishingService providerFundingPublishingService)
        {
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));

            _providerFundingPublishingService = providerFundingPublishingService;
        }

        /// <summary>
        /// Get all published providers (latest version) for a specification
        /// </summary>
        /// <param name="specificationId"></param>
        /// <param name="publishedProviderFundingService"></param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/publishedproviders")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PublishedProviderVersion>))]
        public async Task<IActionResult> PublishedProviders(
            [FromRoute] string specificationId,
            [FromServices] IPublishedProviderFundingService publishedProviderFundingService)
        {
            return await publishedProviderFundingService
                .GetLatestPublishedProvidersForSpecificationId(specificationId);
        }

        /// <summary>
        /// Get published provider - specific version
        /// </summary>
        /// <param name="fundingStreamId">Funding stream Id</param>
        /// <param name="fundingPeriodId">Funding period Id</param>
        /// <param name="providerId">Provider version</param>
        /// <param name="version">Physical version - integer</param>
        /// <returns></returns>
        [HttpGet("api/publishedproviderversions/{fundingStreamId}/{fundingPeriodId}/{providerId}/{version}")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderVersion))]
        public async Task<IActionResult> GetPublishedProviderVersion([FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromRoute] string providerId,
            [FromRoute] string version)
        {
            return await _providerFundingPublishingService.GetPublishedProviderVersion(fundingStreamId,
                fundingPeriodId,
                providerId,
                version);
        }

        /// <summary>
        /// Get all provider versions (summary) for provider for all funding streams
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/publishedprovidertransactions/{specificationId}/{providerId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PublishedProviderTransaction>))]
        public async Task<IActionResult> GetPublishedProviderTransactions([FromRoute] string specificationId,
            [FromRoute] string providerId)
        {
            return await _providerFundingPublishingService.GetPublishedProviderTransactions(specificationId,
                providerId);
        }

        /// <summary>
        /// Get released provider's external API output body JSON
        /// </summary>
        /// <param name="publishedProviderVersionId">Published provider version Id</param>
        /// <param name="publishedProviderVersionService"></param>
        /// <returns></returns>
        [HttpGet("api/publishedproviderversion/{publishedProviderVersionId}/body")]
        [ProducesResponseType(200, Type = typeof(string))]
        [SwaggerOperation(Description = @"Output is schema independent but will be as per the template a time of publish.

The publishedProviderVersionId will be in the context of funding stream ID, funding period Id, provider ID and major/minor version.
")]
        public async Task<IActionResult> GetPublishedProviderVersionBody(
            [FromRoute] string publishedProviderVersionId,
            [FromServices] IPublishedProviderVersionService publishedProviderVersionService)
        {
            return await publishedProviderVersionService.GetPublishedProviderVersionBody(publishedProviderVersionId);
        }

        /// <summary>
        /// Get error summary for all published providers within a specification
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <returns></returns>
        [HttpGet("api/publishedprovidererrors/{specificationId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetPublishedProviderErrors([FromRoute] string specificationId)
        {
            return await _providerFundingPublishingService.GetPublishedProviderErrorSummaries(specificationId);
        }

        /// <summary>
        /// Get count of published provider by state
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <param name="providerType">Optional Provider type</param>
        /// <param name="localAuthority">Optional Local authority</param>
        /// <param name="status">Optional Status</param>
        /// <param name="publishedProviderStatusService"></param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/publishedproviders/publishingstatus")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProviderFundingStreamStatusResponse>))]
        public async Task<IActionResult> GetProviderStatusCounts(
            [FromRoute] string specificationId,
            [FromQuery] string providerType,
            [FromQuery] string localAuthority,
            [FromQuery] string status,
            [FromServices] IPublishedProviderStatusService publishedProviderStatusService)
        {
            return await publishedProviderStatusService
                .GetProviderStatusCounts(specificationId, providerType, localAuthority, status);
        }

        /// <summary>
        ///     Get the funding total and count for providers in the supplied batch
        ///     where they are ready for release
        /// </summary>
        /// <param name="providerIds">the provider ids making up the batch</param>
        /// <param name="specificationId">the specification id to limit the published provider ids to</param>
        /// <param name="publishedProviderStatusService"></param>
        /// <returns>PublishedProviderFundingCount</returns>
        [HttpPost("api/specifications/{specificationId}/publishedproviders/publishingstatus-for-release")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderFundingCount))]
        public async Task<IActionResult> GetProviderBatchForReleaseCount(
            [FromBody] PublishedProviderIdsRequest providerIds,
            [FromRoute] string specificationId,
            [FromServices] IPublishedProviderStatusService publishedProviderStatusService) =>
            await publishedProviderStatusService.GetProviderBatchCountForRelease(providerIds, specificationId);

        /// <summary>
        ///     Get the funding total and count for providers in the supplied batch
        ///     where they are ready for approval
        /// </summary>
        /// <param name="providerIds">the provider ids making up the batch</param>
        /// <param name="specificationId">the specification id to limit the published provider ids to</param>
        /// <param name="publishedProviderStatusService"></param>
        /// <returns>PublishedProviderFundingCount</returns>
        [HttpPost("api/specifications/{specificationId}/publishedproviders/publishingstatus-for-approval")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderFundingCount))]
        public async Task<IActionResult> GetProviderBatchForApprovalCount(
            [FromBody] PublishedProviderIdsRequest providerIds,
            [FromRoute] string specificationId,
            [FromServices] IPublishedProviderStatusService publishedProviderStatusService) =>
            await publishedProviderStatusService.GetProviderBatchCountForApproval(providerIds, specificationId);

        /// <summary>
        /// Delete published provider
        /// </summary>
        /// <param name="fundingStreamId">Funding stream Id</param>
        /// <param name="fundingPeriodId">Funding period Id</param>
        /// <param name="deletePublishedProvidersService"></param>
        /// <param name="featureToggle"></param>
        /// <returns></returns>
        [HttpDelete("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeletePublishedProviders(
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromServices] IDeletePublishedProvidersService deletePublishedProvidersService,
            [FromServices] IFeatureToggle featureToggle)
        {
            if (featureToggle.IsDeletePublishedProviderForbidden())
            {
                return Forbid();
            }

            await deletePublishedProvidersService.QueueDeletePublishedProvidersJob(fundingStreamId,
                fundingPeriodId,
                Request.GetCorrelationId());

            return Ok();
        }
    }
}