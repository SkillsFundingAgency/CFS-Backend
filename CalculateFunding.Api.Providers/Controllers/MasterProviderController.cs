using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
        [ProducesResponseType(200, Type = typeof(ProviderVersion))]
        public async Task<IActionResult> GetAllMasterProviders()
        {
            return await _providerVersionService.GetAllMasterProviders();
        }

        /// <summary>
        /// Search provider versions
        /// </summary>
        /// <returns></returns>
        /// <param name="searchModel">Search Model</param>
        [HttpPost("api/providers/master-search")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResults))]
        public async Task<IActionResult> SearchMasterProviders([FromBody]SearchModel searchModel)
        {
            return await _providerVersionSearchService.SearchMasterProviders(searchModel);
        }

        /// <summary>
        /// Gets a single provider within a provider version
        /// </summary>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/master/{providerId}")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResult))]
        public async Task<IActionResult> GetProviderByIdFromMaster([FromRoute]string providerId)
        {
            return await _providerVersionSearchService.GetProviderByIdFromMaster(providerId);
        }

        /// <summary>
        /// Set a specific version (uploaded via ProviderByVersion) to be the master provider list
        /// </summary>
        /// <param name="masterProviderVersionViewModel"></param>
        /// <returns></returns>
        [HttpPut("api/providers/master")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SetMasterProviderVersion([FromBody]MasterProviderVersionViewModel masterProviderVersionViewModel)
        {
            return await _providerVersionService.SetMasterProviderVersion(masterProviderVersionViewModel);
        }
    }
}
