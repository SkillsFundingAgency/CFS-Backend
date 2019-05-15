using CalculateFunding.Api.Providers.ViewModels;
using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class MasterProviderController : ControllerBase
    {
        /// <summary>
        /// Get the master provider list, containing a list of all the providers in the system with their current information
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/providers/master")]
        [ProducesResponseType(200, Type = typeof(MasterProviderDatasetResultViewModel))]
        public IActionResult GetAllMasterProviders()
        {
            // Lookup which provider version is set to be the master in cache, then fallback to cosmos

            // Use the provider version service  _providerVersionService.GetAllProviders(providerVersionId);

            return Ok(new MasterProviderDatasetResultViewModel());
        }

        /// <summary>
        /// Search provider versions
        /// </summary>
        /// <returns></returns>
        /// <param name="searchModel">Search Model</param>
        [HttpPost("api/providers/master-search")]
        [ProducesResponseType(200, Type = typeof(ProviderSearchResults))]
        public IActionResult SearchMasterProviders([FromBody]SearchModel searchModel)
        {
            // Lookup which provider version is set to be the master in cache, then fallback to cosmos

            // Use the search service _providerVersionSearchService.SearchProviders(providerVersionId, searchModel);

            return Ok(new ProviderSearchResults());
        }

        /// <summary>
        /// Gets a single provider within a provider version
        /// </summary>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/master/{providerId}")]
        [ProducesResponseType(200, Type = typeof(ProviderViewModel))]
        public IActionResult GetProviderByIdFromMaster([FromRoute]string providerId)
        {
            // Lookup which provider version is set to be the master in cache, then fallback to cosmos

            // Use the provider version search service  _providerVersionSearchService.GetProviderById(providerVersionId, providerId);

            return Ok(new ProviderViewModel());
        }

        /// <summary>
        /// Set a specific version (uploaded via ProviderByVersion) to be the master provider list
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        [HttpPut("api/providers/master")]
        [ProducesResponseType(201)]
        public IActionResult SetMasterProviderVersion(SetMasterProviderViewModel configuration)
        {
            return NoContent();
        }
    }
}
