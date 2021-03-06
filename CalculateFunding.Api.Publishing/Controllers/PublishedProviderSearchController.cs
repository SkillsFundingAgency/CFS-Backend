﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishedProviderSearchController : ControllerBase
    {
        private readonly IPublishedSearchService _publishedSearchService;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;

        public PublishedProviderSearchController(
            IPublishedSearchService publishedSearchService,
            IPublishedProviderVersionService publishedProviderVersionService)
        {
            Guard.ArgumentNotNull(publishedSearchService, nameof(publishedSearchService));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));

            _publishedSearchService = publishedSearchService;
            _publishedProviderVersionService = publishedProviderVersionService;
        }

        /// <summary>
        /// Search published providers
        /// </summary>
        /// <param name="searchModel"></param>
        /// <returns></returns>
        [Route("api/publishedprovider/publishedprovider-search")]
        [HttpPost]
        [ProducesResponseType(200, Type = typeof(PublishedSearchResults))]
        public async Task<IActionResult> SearchPublishedProvider([FromBody] SearchModel searchModel)
        {
            return await _publishedSearchService.SearchPublishedProviders(searchModel);
        }

        /// <summary>
        /// Search for published providers, but only return published provider ids
        /// </summary>
        /// <param name="searchModel"></param>
        /// <returns></returns>
        [Route("api/publishedprovider/publishedprovider-id-search")]
        [HttpPost]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> SearchPublishedProviderIds([FromBody] PublishedProviderIdSearchModel searchModel)
        {
            return await _publishedSearchService.SearchPublishedProviderIds(searchModel);
        }

        /// <summary>
        /// Reindex published providers in search index
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/publishedprovider/reindex")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ReIndex()
        {
            return await _publishedProviderVersionService.ReIndex(
                Request.GetUser(),
                Request.GetCorrelationId());
        }

        /// <summary>
        /// Search for a local authority in published provider results
        /// </summary>
        /// <param name="searchText">Search text</param>
        /// <param name="fundingStreamId">Funding stream Id</param>
        /// <param name="fundingPeriodId">Funding period Id</param>
        /// <returns></returns>
        [HttpGet("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}/localauthorities")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> SearchPublishedProviderLocalAuthorities([FromQuery] string searchText, [FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId)
        {
            return await _publishedSearchService.SearchPublishedProviderLocalAuthorities(searchText, fundingStreamId, fundingPeriodId);
        }
    }
}
