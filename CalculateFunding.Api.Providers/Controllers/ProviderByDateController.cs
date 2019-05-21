using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Api.Providers.ViewModels;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class ProviderByDateController : ControllerBase
    {
        private readonly IProviderVersionService _providerVersionService;
        private readonly IProviderVersionSearchService _providerVersionSearchService;

        public ProviderByDateController(IProviderVersionService providerVersionService,
                     IProviderVersionSearchService providerVersionSearchService)
        {
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(providerVersionSearchService, nameof(providerVersionSearchService));

            _providerVersionService = providerVersionService;
            _providerVersionSearchService = providerVersionSearchService;
        }

        /// <summary>
        /// Set provider version associated with specific date
        /// </summary>
        /// <param name="year"></param>
        /// <param name="day"></param>
        /// <param name="month"></param>
        /// <param name="configuration">Provider Date Configuration</param>
        /// <returns></returns>
        [HttpPut("api/providers/date/{year}/{month}/{day}")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SetProviderDateProviderVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day, SetProviderVersionDateViewModel configuration)
        {
            return NoContent();
        }

        /// <summary>
        /// Get all providers for a specific date
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <param name="day">Day</param>
        /// <returns></returns>
        [HttpGet("api/providers/date/{year}/{month}/{day}")]
        [ProducesResponseType(200, Type = typeof(ProviderDatasetResultViewModel))]
        public async Task<IActionResult> GetProvidersByVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day)
        {
            // Lookup which provider version is set to this date in cache, then fallback to lookup in cosmos

            return await _providerVersionService.GetAllProviders(year, month, day);
        }

        /// <summary>
        /// Search providers within the specified Provider Version on this date
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <param name="day">Day</param>
        /// <param name="searchModel">Search model</param>
        /// <returns></returns>
        [HttpPost("api/providers/date-search/{year}/{month}/{day}")]
        [ProducesResponseType(200, Type = typeof(ProviderSearchResults))]
        public async Task<IActionResult> SearchProvidersInProviderVersionAssociatedWithDate([FromRoute]int year, [FromRoute]int month, [FromRoute] int day, [FromBody]SearchModel searchModel)
        {
            // Lookup which provider version is set to this date in cache, then fallback to lookup in cosmos

            return await _providerVersionSearchService.SearchProviders(year, month, day, searchModel);
        }

        /// <summary>
        /// Gets a single provider within a provider version
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <param name="day">Day</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/date/{year}/{month}/{day}/{providerId}")]
        [ProducesResponseType(200, Type = typeof(ProviderViewModel))]
        public async Task<IActionResult> GetProviderByIdFromProviderVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day, [FromRoute]string providerId)
        {
            // Lookup which provider version is set to this date in cache, then fallback to lookup in cosmos

            // Use the provider version search service  _providerVersionSearchService.GetProviderById(providerVersionId, providerId);

            return await _providerVersionSearchService.GetProviderById(year, month, day, providerId);
        }

        /// <summary>
        /// Get available provider lists by month. This lists a summary, not the actual dataset
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <returns></returns>
        [HttpGet("api/providers/date/{year}/{month}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProviderDateInformationViewModel>))]
        public async Task<IActionResult> GetAvailableProvidersByMonth([FromRoute]int year, [FromRoute]int month)
        {
            // Lookup from cosmos to find all dates within them month where there is an associated provider version ID

            return Ok(Enumerable.Empty<ProviderDateInformationViewModel>());
        }
    }
}
