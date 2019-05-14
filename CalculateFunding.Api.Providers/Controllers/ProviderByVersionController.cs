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
        /// <param name="searchModel">Search Model</param>
        /// <returns></returns>
        [HttpPost("api/providers/versions-search")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResults))]
        public IActionResult SearchProviderVersions([FromBody]SearchModel searchModel)
        {
            // Use the search service _providerVersionSearchService.SearchProviderVersions(searchModel);

            return Ok(new ProviderVersionSearchResults());
        }

        /// <summary>
        /// Search providers within the specified Provider Version
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <param name="searchModel">Search model</param>
        /// <returns></returns>
        [HttpGet("api/providers/versions-search/{providerVersionId}")]
        [ProducesResponseType(200, Type = typeof(ProviderSearchResults))]
        public IActionResult SearchProvidersInProviderVersion([FromRoute]string providerVersionId, [FromBody]SearchModel searchModel)
        {
            // Use the search service _providerVersionSearchService.SearchProviders(providerVersionId, searchModel);

            return Ok(new ProviderSearchResults());
        }

        /// <summary>
        /// Get all providers for the given providerVersionId key
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/versions/{providerVersionId}")]
        [ProducesResponseType(200, Type = typeof(ProviderDatasetResultViewModel))]
        public IActionResult GetProvidersByVersion([FromRoute]string providerVersionId)
        {
            // Use the provider version service  _providerVersionService.GetAllProviders(providerVersionId); This will lookup from blob storage

            return Ok(Enumerable.Empty<ProviderViewModel>());
        }

        /// <summary>
        /// Gets a single provider within a provider version
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/versions/{providerVersionId}/{providerId}")]
        [ProducesResponseType(200, Type = typeof(ProviderViewModel))]
        public IActionResult GetProviderByIdFromProviderVersion([FromRoute]string providerVersionId, [FromRoute]string providerId)
        {
            // Use the provider version search service  _providerVersionSearchService.GetProviderById(providerVersionId, providerId);

            return Ok(new ProviderViewModel());
        }

        /// <summary>
        /// Create a new provider version list with key of given providerVersionId
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <param name="providers">List of Providers</param>
        /// <returns></returns>
        [HttpPost("api/providers/versions/{providerVersionId}")]
        public IActionResult UploadProviderVersion([FromRoute]string providerVersionId, [FromBody]ProviderUploadViewModel providers)
        {
            return CreatedAtAction(nameof(GetProvidersByVersion), new { providerVersionId = providerVersionId });
        }
    }
}
