using System.Linq;
using CalculateFunding.Api.Providers.ViewModels;
using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class ProviderByVersionController : ControllerBase
    {
        /// <summary>
        /// Search provider versions
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/version")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResults))]
        public IActionResult GetProvidersByVersion([FromBody]SearchModel searchModel)
        {
            return Ok(new ProviderVersionSearchResults());
        }

        /// <summary>
        /// Get all providers for the given providerVersionId key
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/version/{providerVersionId}")]
        [ProducesResponseType(200, Type = typeof(ProviderDatasetResultViewModel))]
        public IActionResult GetProvidersByVersion([FromRoute]string providerVersionId)
        {
            return Ok(Enumerable.Empty<ProviderViewModel>());
        }

        /// <summary>
        /// Create a new provider version list with key of given providerVersionId
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <param name="providers">List of Providers</param>
        /// <returns></returns>
        [HttpPost("api/providers/version/{providerVersionId}")]
        public IActionResult UploadProviderVersion([FromRoute]string providerVersionId, [FromBody]ProviderUploadViewModel providers)
        {
            return CreatedAtAction(nameof(GetProvidersByVersion), new { providerVersionId = providerVersionId });
        }
    }
}
