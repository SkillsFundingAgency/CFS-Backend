using CalculateFunding.Api.Providers.ViewModels;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class MasterProviderController : ControllerBase
    {
        private readonly IProviderVersionService _providerVersionService;
        private readonly IProviderVersionSearchService _providerVersionSearchService;

        public MasterProviderController(IProviderVersionService providerVersionService,
             IProviderVersionSearchService providerVersionSearchService)
        {
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(providerVersionSearchService, nameof(providerVersionSearchService));

            _providerVersionService = providerVersionService;
            _providerVersionSearchService = providerVersionSearchService;
        }

        /// <summary>
        /// Get the master provider list, containing a list of all the providers in the system with their current information
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/providers/master")]
        [ProducesResponseType(200, Type = typeof(MasterProviderDatasetResultViewModel))]
        public async Task<IActionResult> GetAllMasterProviders()
        {
            // Lookup which provider version is set to be the master in cache, then fallback to cosmos

            return await _providerVersionService.GetAllProviders(null);
        }

        /// <summary>
        /// Search provider versions
        /// </summary>
        /// <returns></returns>
        /// <param name="searchModel">Search Model</param>
        [HttpPost("api/providers/master-search")]
        [ProducesResponseType(200, Type = typeof(ProviderSearchResults))]
        public async Task<IActionResult> SearchMasterProviders([FromBody]SearchModel searchModel)
        {
            // Lookup which provider version is set to be the master in cache, then fallback to cosmos

            return await _providerVersionSearchService.SearchProviders(null, searchModel);
        }

        /// <summary>
        /// Gets a single provider within a provider version
        /// </summary>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/master/{providerVersionId}/{providerId}")]
        [ProducesResponseType(200, Type = typeof(ProviderViewModel))]
        public async Task<IActionResult> GetProviderByIdFromMaster([FromRoute]string providerId)
        {
            // Lookup which provider version is set to be the master in cache, then fallback to cosmos

            // Use the provider version search service  _providerVersionSearchService.GetProviderById(providerVersionId, providerId);

            return await _providerVersionSearchService.GetProviderById(null, providerId);
        }

        /// <summary>
        /// Set a specific version (uploaded via ProviderByVersion) to be the master provider list
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        [HttpPut("api/providers/master")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SetMasterProviderVersion([FromBody]SetMasterProviderViewModel configuration)
        {
            return NoContent();
        }
    }
}
