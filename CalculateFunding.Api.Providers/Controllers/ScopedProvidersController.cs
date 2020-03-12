using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class ScopedProvidersController : ControllerBase
    {
        private readonly IScopedProvidersService _scopedProvidersService;

        public ScopedProvidersController(IScopedProvidersService scopedProvidersService)
        {
            Guard.ArgumentNotNull(scopedProvidersService, nameof(scopedProvidersService));

            _scopedProvidersService = scopedProvidersService;
        }

        [HttpGet("api/scopedproviders/get-provider-summaries/{specificationId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProviderSummary>))]
        public async Task<IActionResult> FetchCoreProviderData(string specificationId)
        {
            return await _scopedProvidersService.FetchCoreProviderData(specificationId);
        }

        [HttpGet("api/scopedproviders/set-cached-providers/{specificationId}/{setCachedProviders}")]
        [ProducesResponseType(200, Type = typeof(int?))]
        public async Task<IActionResult> PopulateProviderSummariesForSpecification([FromRoute]string specificationId, [FromRoute]bool setCachedProviders)
        {
            return await _scopedProvidersService.PopulateProviderSummariesForSpecification(specificationId, setCachedProviders: setCachedProviders);
        }

        [HttpGet("api/scopedproviders/get-provider-ids/{specificationId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetScopedProviderIds(string specificationId)
        {
            return await _scopedProvidersService.GetScopedProviderIds(specificationId);
        }
    }
}