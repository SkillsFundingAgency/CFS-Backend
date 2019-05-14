using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Api.Providers.ViewModels;
using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class ProviderByDateController : ControllerBase
    {
        /// <summary>
        /// Set provider version associated with specific date
        /// </summary>
        /// <param name="configuration">Provider Date Configuration</param>
        /// <returns></returns>
        [HttpPut("api/providers/date/{year}/{month}/{day}")]
        [ProducesResponseType(201)]
        public IActionResult SetProviderDateProviderVersion(SetProviderVersionDateViewModel configuration)
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
        public IActionResult GetProvidersByVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day)
        {
            // Lookup which provider version is set to this date in cache, then fallback to lookup in cosmos

            // Use the provider version service  _providerVersionService.GetAllProviders(providerVersionId);

            return Ok(new ProviderDatasetResultViewModel());
        }

        /// <summary>
        /// Search providers within the specified Provider Version on this date
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <param name="day">Day</param>
        /// <param name="searchModel">Search model</param>
        /// <returns></returns>
        [HttpGet("api/providers/date-search/{year}/{month}/{day}")]
        [ProducesResponseType(200, Type = typeof(ProviderSearchResults))]
        public IActionResult SearchProvidersInProviderVersionAssociatedWithDate([FromRoute]int year, [FromRoute]int month, [FromRoute] int day, [FromBody]SearchModel searchModel)
        {
            // Lookup which provider version is set to this date in cache, then fallback to lookup in cosmos

            // Use the search service _providerVersionSearchService.SearchProviders(providerVersionId, searchModel);

            return Ok(new ProviderSearchResults());
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
        public IActionResult GetProviderByIdFromProviderVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day, [FromRoute]string providerId)
        {
            // Lookup which provider version is set to this date in cache, then fallback to lookup in cosmos

            // Use the provider version search service  _providerVersionSearchService.GetProviderById(providerVersionId, providerId);

            return Ok(new ProviderViewModel());
        }

        /// <summary>
        /// Get available provider lists by month. This lists a summary, not the actual dataset
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <returns></returns>
        [HttpGet("api/providers/date/{year}/{month}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProviderDateInformationViewModel>))]
        public IActionResult GetAvailableProvidersByMonth([FromRoute]int year, [FromRoute]int month)
        {
            // Lookup from cosmos to find all dates within them month where there is an associated provider version ID

            return Ok(Enumerable.Empty<ProviderDateInformationViewModel>());
        }
    }
}
