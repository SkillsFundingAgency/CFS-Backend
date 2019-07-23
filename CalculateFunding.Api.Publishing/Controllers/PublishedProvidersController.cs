using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishedProvidersController : Controller
    {
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;

        public PublishedProvidersController(
            IProviderFundingPublishingService providerFundingPublishingService,
            IPublishedProviderVersionService publishedProviderVersionService)
        {
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));

            _providerFundingPublishingService = providerFundingPublishingService;
            _publishedProviderVersionService = publishedProviderVersionService;
        }

        /// <summary>
        /// Get published provider version
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/publishedproviderversions/{fundingStreamId}/{fundingPeriodId}/{providerId}/{version}")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderVersion))]
        public async Task<IActionResult> GetPublishedProviderVersion([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string providerId, [FromRoute] string version)
        {
            return await _providerFundingPublishingService.GetPublishedProviderVersion(fundingStreamId,
                fundingPeriodId,
                providerId,
                version);
        }

        /// <summary>
        /// Get published provider version body
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/publishedproviderversion/{publishedProviderVersionId}/body")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderVersion))]
        public async Task<IActionResult> GetPublishedProviderVersionBody([FromRoute] string publishedProviderVersionId)
        {
            return await _publishedProviderVersionService.GetPublishedProviderVersionBody(publishedProviderVersionId);
        }
    }
}