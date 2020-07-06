using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class ProvidersController : ControllerBase
    {
        private readonly IProviderVersionSearchService _providerVersionSearchService;

        public ProvidersController(IProviderVersionSearchService providerVersionSearchService)
        {
            Guard.ArgumentNotNull(providerVersionSearchService, nameof(providerVersionSearchService));
            
            _providerVersionSearchService = providerVersionSearchService;
        }

        /// <summary>
        /// Get list of local authorities
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/local-authorities")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetLocalAuthorityNames()
        {
            return await _providerVersionSearchService.GetFacetValues("authority");
        }

        /// <summary>
        /// Get list of local authorities for provider version id
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/local-authorities/versions/{providerVersionId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetLocalAuthorityNamesByProviderVersionId([FromRoute]string providerVersionId)
        {
            return await _providerVersionSearchService.GetLocalAuthoritiesByProviderVersionId(providerVersionId);
        }

        /// <summary>
        /// Get list of local authorities for funding stream id
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/local-authorities/fundingstreams/{fundingStreamId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetLocalAuthorityNamesByFundingStreamId([FromRoute]string fundingStreamId)
        {
            return await _providerVersionSearchService.GetLocalAuthoritiesByFundingStreamId(fundingStreamId);
        }
    }
}
