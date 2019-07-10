using System.Collections.Generic;
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
        [HttpPost("api/providers/versions-search/{providerVersionId}")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResults))]
        public async Task<IActionResult> SearchProvidersInProviderVersion([FromRoute]string providerVersionId, [FromBody]SearchModel searchModel)
        {
            return await _providerVersionSearchService.SearchProviders(providerVersionId, searchModel);
        }

        /// <summary>
        /// Does provider verion exist for specified Provider Version
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <returns></returns>
        [HttpHead("api/providers/versions/{providerVersionId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DoesProviderVersionExist([FromRoute]string providerVersionId)
        {
            return await _providerVersionService.DoesProviderVersionExist(providerVersionId);
        }

        /// <summary>
        /// Get all providers for the given providerVersionId key
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/versions/{providerVersionId}")]
        [ProducesResponseType(200, Type = typeof(ProviderVersion))]
        public async Task<IActionResult> GetProvidersByVersion([FromRoute]string providerVersionId)
        {
            return await _providerVersionService.GetAllProviders(providerVersionId);
        }

        /// <summary>
        /// Gets a single provider within a provider version
        /// </summary>
        /// <param name="providerVersionId">Provider Version Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/providers/versions/{providerVersionId}/{providerId}")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResult))]
        public async Task<IActionResult> GetProviderByIdFromProviderVersion([FromRoute]string providerVersionId, [FromRoute]string providerId)
        {
            return await _providerVersionSearchService.GetProviderById(providerVersionId, providerId);
        }

        [HttpGet("api/providers/versions-by-fundingstream/{fundingStreamId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProviderVersion>))]
        public async Task<IActionResult> GetProviderVersions([FromRoute]string fundingStreamId)
        {
            return await _providerVersionService.GetProviderVersions(fundingStreamId);
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

            if (ControllerContext.RouteData.Values.ContainsKey("controller"))
            {
                controllerName = (string)ControllerContext.RouteData.Values["controller"];
            }

            return await _providerVersionService.UploadProviderVersion(nameof(GetProvidersByVersion), controllerName, providerVersionId, providers);
        }
    }
}
