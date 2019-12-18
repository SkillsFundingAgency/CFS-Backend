using System.Threading.Tasks;
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
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IDeletePublishedProvidersService _deletePublishedProvidersService;

        public PublishedProvidersController(
            IProviderFundingPublishingService providerFundingPublishingService,
            IPublishedProviderVersionService publishedProviderVersionService, 
            IDeletePublishedProvidersService deletePublishedProvidersService)
        {
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(deletePublishedProvidersService, nameof(deletePublishedProvidersService));

            _providerFundingPublishingService = providerFundingPublishingService;
            _publishedProviderVersionService = publishedProviderVersionService;
            _deletePublishedProvidersService = deletePublishedProvidersService;
        }
        
        [HttpDelete("api/publishedproviderversions/{fundingStreamId}/{fundingPeriodId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeletePublishedProviders([FromRoute] string fundingStreamId, 
            [FromRoute] string fundingPeriodId)
        {
            await _deletePublishedProvidersService.QueueDeletePublishedProvidersJob(fundingStreamId,
                fundingPeriodId,
                Request.GetUser(),
                Request.GetCorrelationId());

            return NoContent();
        }

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

        [HttpGet("api/publishedproviderversion/{publishedProviderVersionId}/body")]
        [ProducesResponseType(200, Type = typeof(PublishedProviderVersion))]
        public async Task<IActionResult> GetPublishedProviderVersionBody([FromRoute] string publishedProviderVersionId)
        {
            return await _publishedProviderVersionService.GetPublishedProviderVersionBody(publishedProviderVersionId);
        }
    }
}