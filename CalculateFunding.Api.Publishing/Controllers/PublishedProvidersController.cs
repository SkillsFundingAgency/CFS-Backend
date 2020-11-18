using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CacheCow.Server.Core.Mvc;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishedProvidersController : ControllerBase
    {
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;
        private readonly IPublishedProviderStatusService _publishedProviderStatusService;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPublishedProviderFundingService _publishedProviderFundingService;
        private readonly IPublishedProviderFundingStructureService _publishedProviderFundingStructureService;
        private readonly IDeletePublishedProvidersService _deletePublishedProvidersService;
        private readonly IPublishedProviderUpdateDateService _publishedProviderUpdateDateService;
        private readonly IFeatureToggle _featureToggle;

        public PublishedProvidersController(IProviderFundingPublishingService providerFundingPublishingService,
            IPublishedProviderStatusService publishedProviderStatusService,
            IPublishedProviderVersionService publishedProviderVersionService,
            IPublishedProviderFundingService publishedProviderFundingService,
            IPublishedProviderFundingStructureService publishedProviderFundingStructureService,
            IDeletePublishedProvidersService deletePublishedProvidersService,
            IPublishedProviderUpdateDateService publishedProviderUpdateDateService,
            IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));
            Guard.ArgumentNotNull(publishedProviderStatusService, nameof(publishedProviderStatusService));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(publishedProviderFundingService, nameof(publishedProviderFundingService));
            Guard.ArgumentNotNull(publishedProviderFundingStructureService, nameof(publishedProviderFundingStructureService));
            Guard.ArgumentNotNull(deletePublishedProvidersService, nameof(deletePublishedProvidersService));
            Guard.ArgumentNotNull(publishedProviderUpdateDateService, nameof(publishedProviderUpdateDateService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _providerFundingPublishingService = providerFundingPublishingService;
            _publishedProviderStatusService = publishedProviderStatusService;
            _publishedProviderVersionService = publishedProviderVersionService;
            _publishedProviderFundingService = publishedProviderFundingService;
            _publishedProviderFundingStructureService = publishedProviderFundingStructureService;
            _deletePublishedProvidersService = deletePublishedProvidersService;
            _publishedProviderUpdateDateService = publishedProviderUpdateDateService;
            _featureToggle = featureToggle;
        }

        /// <summary>
        ///     Query the latest updated date for published providers
        ///     with funding streams and funding periods matching the supplied value
        /// </summary>
        [HttpGet("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}/lastupdated")]
        [ProducesResponseType(200, Type = typeof(DateTime?))]
        public async Task<IActionResult> GetLatestPublishedDate([FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId)
            => await _publishedProviderUpdateDateService.GetLatestPublishedDate(fundingStreamId, fundingPeriodId);

        /// <summary>
        /// Get all published providers (latest version) for a specification
        /// </summary>
        /// <param name="specificationId"></param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/publishedproviders")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PublishedProviderVersion>))]
        public async Task<IActionResult> PublishedProviders(
            [FromRoute] string specificationId) =>
            await _publishedProviderFundingService
                .GetLatestPublishedProvidersForSpecificationId(specificationId);

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
            [FromRoute] string version) => 
            await _providerFundingPublishingService.GetPublishedProviderVersion(fundingStreamId,
                fundingPeriodId,
                providerId,
                version);

        /// <summary>
        /// Get all provider versions (summary) for provider for all funding streams
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/publishedprovidertransactions/{specificationId}/{providerId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PublishedProviderTransaction>))]
        public async Task<IActionResult> GetPublishedProviderTransactions([FromRoute] string specificationId,
            [FromRoute] string providerId) =>
            await _providerFundingPublishingService.GetPublishedProviderTransactions(specificationId,providerId);

        /// <summary>
        /// Get released provider's external API output body JSON
        /// </summary>
        /// <param name="publishedProviderVersionId">Published provider version Id</param>
        /// <returns></returns>
        [HttpGet("api/publishedproviderversion/{publishedProviderVersionId}/body")]
        [ProducesResponseType(200, Type = typeof(string))]
        [SwaggerOperation(Description = @"Output is schema independent but will be as per the template a time of publish.

The publishedProviderVersionId will be in the context of funding stream ID, funding period Id, provider ID and major/minor version.
")]
        public async Task<IActionResult> GetPublishedProviderVersionBody(
            [FromRoute] string publishedProviderVersionId) =>
            await _publishedProviderVersionService.GetPublishedProviderVersionBody(publishedProviderVersionId);

        /// <summary>
        /// Get error summary for all published providers within a specification
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <returns></returns>
        [HttpGet("api/publishedprovidererrors/{specificationId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetPublishedProviderErrors([FromRoute] string specificationId) =>
            await _providerFundingPublishingService.GetPublishedProviderErrorSummaries(specificationId);

        /// <summary>
        /// Get count of published provider by state
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <param name="providerType">Optional Provider type</param>
        /// <param name="localAuthority">Optional Local authority</param>
        /// <param name="status">Optional Status</param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/publishedproviders/publishingstatus")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProviderFundingStreamStatusResponse>))]
        public async Task<IActionResult> GetProviderStatusCounts(
            [FromRoute] string specificationId,
            [FromQuery] string providerType,
            [FromQuery] string localAuthority,
            [FromQuery] string status) =>
            await _publishedProviderStatusService.GetProviderStatusCounts(specificationId, providerType, localAuthority, status);

        /// <summary>
        ///     Get the funding total and count for providers in the supplied batch
        ///     where they are ready for release
        /// </summary>
        /// <param name="providerIds">the provider ids making up the batch</param>
        /// <param name="specificationId">the specification id to limit the published provider ids to</param>
        /// <returns>PublishedProviderFundingCount</returns>
        [HttpPost("api/specifications/{specificationId}/publishedproviders/publishingstatus-for-release")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderFundingCount))]
        public async Task<IActionResult> GetProviderBatchForReleaseCount(
            [FromBody] PublishedProviderIdsRequest providerIds,
            [FromRoute] string specificationId) =>
            await _publishedProviderStatusService.GetProviderBatchCountForRelease(providerIds, specificationId);

        /// <summary>
        ///     Get the funding total and count for providers in the supplied batch
        ///     where they are ready for approval
        /// </summary>
        /// <param name="providerIds">the provider ids making up the batch</param>
        /// <param name="specificationId">the specification id to limit the published provider ids to</param>
        /// <returns>PublishedProviderFundingCount</returns>
        [HttpPost("api/specifications/{specificationId}/publishedproviders/publishingstatus-for-approval")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderFundingCount))]
        public async Task<IActionResult> GetProviderBatchForApprovalCount(
            [FromBody] PublishedProviderIdsRequest providerIds,
            [FromRoute] string specificationId) =>
            await _publishedProviderStatusService.GetProviderBatchCountForApproval(providerIds, specificationId);

        /// <summary>
        /// Delete published provider
        /// </summary>
        /// <param name="fundingStreamId">Funding stream Id</param>
        /// <param name="fundingPeriodId">Funding period Id</param>
        /// <returns></returns>
        [HttpDelete("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeletePublishedProviders(
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId)
        {
            if (_featureToggle.IsDeletePublishedProviderForbidden())
            {
                return Forbid();
            }

            await _deletePublishedProvidersService.QueueDeletePublishedProvidersJob(fundingStreamId,
                fundingPeriodId,
                Request.GetCorrelationId());

            return Ok();
        }

        /// <summary>
        ///  Get the Published Provider Funding Structure by published provider version id
        /// </summary>
        /// <param name="publishedProviderVersionId">publishedProviderVersionId</param>
        /// <returns>PublishedProviderFundingStructure</returns>
        [HttpGet("api/publishedproviderfundingstructure/{publishedProviderVersionId}")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderFundingStructure))]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [HttpCacheFactory(0, ViewModelType = typeof(PublishedProviderFundingStructure))]
        public async Task<IActionResult> GetPublishedProviderFundingStructure(
            [FromRoute]string publishedProviderVersionId) =>
            await _publishedProviderFundingStructureService.GetPublishedProviderFundingStructure(publishedProviderVersionId);

        /// <summary>
        ///     Generates a csv file for given providers where they are ready for approval
        /// </summary>
        /// <param name="providerIds">the provider ids making up the batch</param>
        /// <param name="specificationId">the specification id to limit the published provider ids to</param>
        /// <returns>Url for generated CSV file that is stored in blob storage</returns>
        [HttpPost("api/specifications/{specificationId}/publishedproviders/generate-csv-for-approval")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderDataDownload))]
        public async Task<IActionResult> GenerateCsvForPublishedProvidersForApproval(
            [FromBody] PublishedProviderIdsRequest providerIds,
            [FromRoute] string specificationId)
        {
            return await _publishedProviderStatusService.GetProviderDataForApprovalAsCsv(providerIds, specificationId);
        }

        /// <summary>
        ///     Generates a csv file for given providers where they are ready for release
        /// </summary>
        /// <param name="providerIds">the provider ids making up the batch</param>
        /// <param name="specificationId">the specification id to limit the published provider ids to</param>
        /// <returns>Url for generated CSV file that is stored in blob storage</returns>
        [HttpPost("api/specifications/{specificationId}/publishedproviders/generate-csv-for-release")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderDataDownload))]
        public async Task<IActionResult> GenerateCsvForPublishedProvidersForRelease(
            [FromBody] PublishedProviderIdsRequest providerIds,
            [FromRoute] string specificationId)
        {
            return await _publishedProviderStatusService.GetProviderDataForReleaseAsCsv(providerIds, specificationId);

        }
    }
}