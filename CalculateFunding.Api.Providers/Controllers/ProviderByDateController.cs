using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Api.Providers.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class ProviderByDateController : ControllerBase
    {
        /// <summary>
        /// Get Provider Version for specific date
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month</param>
        /// <param name="day">Day</param>
        /// <returns></returns>
        [HttpGet("api/providers/date/{year}/{month}/{day}")]
        [ProducesResponseType(200, Type = typeof(ProviderDatasetResultViewModel))]
        public IActionResult GetProvidersByVersion([FromRoute]int year, [FromRoute]int month, [FromRoute] int day)
        {
            return Ok(new ProviderDatasetResultViewModel());
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
            return Ok(Enumerable.Empty<ProviderDateInformationViewModel>());
        }

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
    }
}
