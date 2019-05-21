using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CalculateFunding.Api.Providers.ViewModels;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class ProviderByVersionController : ControllerBase
    {
        private readonly IProviderVersionService _providerVersionService;
        private readonly IProviderVersionSearchService _providerVersionSearchService;

        public ProviderByVersionController(IProviderVersionService providerVersionService,
                     IProviderVersionSearchService providerVersionSearchService)
        {
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(providerVersionSearchService, nameof(providerVersionSearchService));

            _providerVersionService = providerVersionService;
            _providerVersionSearchService = providerVersionSearchService;
        }

        /// <summary>
        /// Search provider versions
        /// </summary>
        /// <param name="searchModel">Search Model</param>
        /// <returns></returns>
        [HttpPost("api/providers/versions-search")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResults))]
        public async Task<IActionResult> SearchProviderVersions([FromBody]SearchModel searchModel)
        {
            return await _providerVersionSearchService.SearchProviderVersions(searchModel);
        }

        /// <summary>
        /// Search providers within the specified Provider Version
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <param name="searchModel">Search model</param>
        /// <returns></returns>
        [HttpGet("api/providers/versions-search/{providerVersionId}")]
        [ProducesResponseType(200, Type = typeof(ProviderSearchResults))]
        public async Task<IActionResult> SearchProvidersInProviderVersion([FromRoute]string providerVersionId, [FromBody]SearchModel searchModel)
        {
            return await _providerVersionSearchService.SearchProviders(providerVersionId, searchModel);
        }

        /// <summary>
        /// Get all providers for the given providerVersionId key
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/versions/{providerVersionId}")]
        [ProducesResponseType(200, Type = typeof(ProviderDatasetResultViewModel))]
        public async Task<IActionResult> GetProvidersByVersion([FromRoute]string providerVersionId)
        {
            // Use the provider version service  _providerVersionService.GetAllProviders(providerVersionId); This will lookup from blob storage

            return await _providerVersionService.GetAllProviders(providerVersionId);
        }

        /// <summary>
        /// Gets a single provider within a provider version
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/versions/{providerVersionId}/{providerId}")]
        [ProducesResponseType(200, Type = typeof(ProviderViewModel))]
        public async Task<IActionResult> GetProviderByIdFromProviderVersion([FromRoute]string providerVersionId, [FromRoute]string providerId)
        {
            // Use the provider version search service  _providerVersionSearchService.GetProviderById(providerVersionId, providerId);

            return await _providerVersionSearchService.GetProviderById(providerVersionId, providerId);
        }

        /// <summary>
        /// Create a new provider version list with key of given providerVersionId
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <param name="providers">List of Providers</param>
        /// <returns></returns>
        [HttpPost("api/providers/versions/{providerVersionId}")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadProviderVersion([FromRoute]string providerVersionId, [FromBody]ProviderVersionViewModel providers)
        {
            string controllerName = string.Empty;

            if (this.ControllerContext.RouteData.Values.ContainsKey("controller"))
            {
                controllerName = (string)this.ControllerContext.RouteData.Values["controller"];
            }

            return await _providerVersionService.UploadProviderVersion(nameof(GetProvidersByVersion), controllerName, providerVersionId, providers);
        }
    }
}
