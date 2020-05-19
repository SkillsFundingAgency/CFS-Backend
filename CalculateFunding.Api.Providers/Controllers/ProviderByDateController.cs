using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Repositories.Common.Search.Results;
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
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <returns></returns>
        [HttpPut("api/providers/date/{year}/{month}/{day}")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SetProviderDateProviderVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day, string providerVersionId)
        {
            return await _providerVersionService.SetProviderVersionByDate(year, month, day, providerVersionId);
        }

        /// <summary>
        /// Get all providers for a specific date
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <param name="day">Day</param>
        /// <returns></returns>
        [HttpGet("api/providers/date/{year}/{month}/{day}")]
        [ProducesResponseType(200, Type = typeof(ProviderVersion))]
        public async Task<IActionResult> GetProvidersByVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day)
        {
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
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResults))]
        public async Task<IActionResult> SearchProvidersInProviderVersionAssociatedWithDate([FromRoute]int year, [FromRoute]int month, [FromRoute] int day, [FromBody]SearchModel searchModel)
        {
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
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResult))]
        public async Task<IActionResult> GetProviderByIdFromProviderVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day, [FromRoute]string providerId)
        {
            return await _providerVersionSearchService.GetProviderById(year, month, day, providerId);
        }

        /// <summary>
        /// Get available provider lists by month. This lists a summary, not the actual dataset
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <returns></returns>
        [HttpGet("api/providers/date/{year}/{month}")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResults))]
        public IActionResult GetAvailableProvidersByMonth([FromRoute]int year, [FromRoute]int month)
        {
            return Ok(Enumerable.Empty<ProviderVersionSearchResults>());
        }
    }
}
