using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishedProvidersController : ControllerBase
    {
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IDeletePublishedProvidersService _deletePublishedProvidersService;
        private readonly ICustomProfileService _customProfileService;
        private readonly IProfileTotalsService _profileTotalsService;
        private readonly IPublishedProviderProfilingService _publishedProviderProfilingService;
        private readonly IFeatureToggle _featureToggle;

        public PublishedProvidersController(IProviderFundingPublishingService providerFundingPublishingService,
            IPublishedProviderVersionService publishedProviderVersionService,
            IDeletePublishedProvidersService deletePublishedProvidersService,
            IProfileTotalsService profileTotalsService,
            IPublishedProviderProfilingService publishedProviderProfilingService,
            ICustomProfileService customProfileService,
            IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(deletePublishedProvidersService, nameof(deletePublishedProvidersService));
            Guard.ArgumentNotNull(customProfileService, nameof(customProfileService));
            Guard.ArgumentNotNull(profileTotalsService, nameof(profileTotalsService));
            Guard.ArgumentNotNull(publishedProviderProfilingService, nameof(publishedProviderProfilingService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _providerFundingPublishingService = providerFundingPublishingService;
            _publishedProviderVersionService = publishedProviderVersionService;
            _deletePublishedProvidersService = deletePublishedProvidersService;
            _featureToggle = featureToggle;
            _customProfileService = customProfileService;
            _profileTotalsService = profileTotalsService;
            _publishedProviderProfilingService = publishedProviderProfilingService;
        }
        
        [HttpGet("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}/{providerId}/profileTotals")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProfileTotal>))]
        public async Task<IActionResult> GetLatestProfileTotalsForPublishedProvider([FromRoute] string fundingStreamId, 
            [FromRoute] string fundingPeriodId,
            [FromRoute] string providerId)
        {
            return await _profileTotalsService.GetPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);
        }

        [HttpGet("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}/{providerId}/allProfileTotals")]
        [ProducesResponseType(200, Type = typeof(IDictionary<int, ProfilingVersion>))]
        public async Task<IActionResult> GetAllReleasedProfileTotalsForPublishedProvider([FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromRoute] string providerId)
        {
            return await _profileTotalsService.GetAllReleasedPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);
        }

        [HttpDelete("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeletePublishedProviders([FromRoute] string fundingStreamId, 
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

        [HttpGet("api/publishedprovidertransactions/{specificationId}/{providerId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<PublishedProviderTransaction>))]
        public async Task<IActionResult> GetPublishedProviderTransactions([FromRoute] string specificationId,
            [FromRoute] string providerId)
        {
            return await _providerFundingPublishingService.GetPublishedProviderTransactions(specificationId,
                providerId);
        }

        [HttpGet("api/publishedproviderversion/{publishedProviderVersionId}/body")]
        [ProducesResponseType(200, Type = typeof(string))]
        public async Task<IActionResult> GetPublishedProviderVersionBody([FromRoute] string publishedProviderVersionId)
        {
            return await _publishedProviderVersionService.GetPublishedProviderVersionBody(publishedProviderVersionId);
        }

        [HttpPost("api/publishedprovider/fundingStream/{fundingStreamId}/fundingPeriod/{fundingPeriodId}/provider/{providerId}")]
        [ProducesResponseType(200, Type = typeof(HttpStatusCode))]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]

        public async Task<IActionResult> AssignProfilePatternKeyToPublishedProvider(
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromRoute] string providerId,
            [FromBody] ProfilePatternKey profilePatternKey)
        {
            return await _publishedProviderProfilingService.AssignProfilePatternKey(
                fundingStreamId, fundingPeriodId, providerId, profilePatternKey, Request.GetUserOrDefault());
        }

        [HttpPost("api/publishedproviders/customprofiles")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ApplyCustomProfilePattern([FromBody] ApplyCustomProfileRequest request)
        {
            return await _customProfileService.ApplyCustomProfile(request, Request.GetUser());
        }
    }
}