using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Publishing.FundingManagement;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class ReleaseManagementActionsController : ControllerBase
    {
        private readonly IReleaseProvidersToChannelsService _releaseProvidersToChannelsService;
        private readonly IPublishedProviderStatusService _publishedProviderStatusService;

        public ReleaseManagementActionsController(
            IReleaseProvidersToChannelsService releaseProvidersToChannelsService,
            IPublishedProviderStatusService publishedProviderStatusService)
        {
            _releaseProvidersToChannelsService = releaseProvidersToChannelsService;
            _publishedProviderStatusService = publishedProviderStatusService;
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

        /// <summary>
        ///     Get the funding and provider summary for the supplied providers where they are approved and ready for release
        /// </summary>
        /// <param name="specificationId">The specification id for the release funding summary</param>
        /// <param name="request">The provider ids (if empty uses all providers in spec) and list of channels to include in the release funding summary</param>
        /// <returns>PublishedProviderFundingCount</returns>
        [HttpPost("api/specifications/{specificationId}/publishedproviders/release-funding-summary")]
        [ProducesResponseType(200, Type = typeof(ReleaseFundingPublishedProvidersSummary))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetApprovedPublishedProvidersReleaseFundingSummary(
            [FromBody] ReleaseFundingPublishProvidersRequest request,
            [FromRoute] string specificationId) =>
            await _publishedProviderStatusService.GetApprovedPublishedProviderReleaseFundingSummary(request, specificationId);

        /// <summary>
        /// Get all provider versions (summary) for provider for all funding streams
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/provider/{providerId}/publishedprovidertransactions")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PublishedProviderTransaction>))]
        public async Task<IActionResult> GetPublishedProviderTransactions([FromRoute] string specificationId,
            [FromRoute] string providerId) =>
            await _publishedProviderStatusService.GetPublishedProviderTransactions(specificationId, providerId);
    }
}
