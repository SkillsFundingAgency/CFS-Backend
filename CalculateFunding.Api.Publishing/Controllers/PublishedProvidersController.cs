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
    public class PublishedProvidersController : Controller
    {
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;

        public PublishedProvidersController(IProviderFundingPublishingService providerFundingPublishingService)
        {
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));

            _providerFundingPublishingService = providerFundingPublishingService;
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
    }
}