﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishedProvidersController : ControllerBase
    {
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IDeletePublishedProvidersService _deletePublishedProvidersService;
        private readonly IFeatureToggle _featureToggle;

        public PublishedProvidersController(
            IProviderFundingPublishingService providerFundingPublishingService,
            IPublishedProviderVersionService publishedProviderVersionService, 
            IDeletePublishedProvidersService deletePublishedProvidersService, 
            IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(deletePublishedProvidersService, nameof(deletePublishedProvidersService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _providerFundingPublishingService = providerFundingPublishingService;
            _publishedProviderVersionService = publishedProviderVersionService;
            _deletePublishedProvidersService = deletePublishedProvidersService;
            _featureToggle = featureToggle;
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
        [ProducesResponseType(200, Type = typeof(PublishedProviderVersion))]
        public async Task<IActionResult> GetPublishedProviderVersionBody([FromRoute] string publishedProviderVersionId)
        {
            return await _publishedProviderVersionService.GetPublishedProviderVersionBody(publishedProviderVersionId);
        }
    }
}