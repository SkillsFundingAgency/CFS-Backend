using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V3.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiController]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/funding/provider")]
    public class ProviderFundingVersionController : ControllerBase
    {
        private readonly IProviderFundingVersionService _providerFundingVersionService;

        public ProviderFundingVersionController(IProviderFundingVersionService providerFundingVersionService)
        {
            Guard.ArgumentNotNull(providerFundingVersionService, nameof(providerFundingVersionService));

            _providerFundingVersionService = providerFundingVersionService;
        }

        /// <summary>
        /// Gets provider funding version based on key from notification feed
        /// </summary>
        /// <param name="providerFundingVersion">Provider Funding Version Key</param>
        /// <returns>Provider Version contents</returns>
        [HttpGet("{providerFundingVersion}")]
        [Produces(typeof(object))]
        public async Task<IActionResult> GetFunding([FromRoute] string providerFundingVersion)
        {
            return await _providerFundingVersionService.GetProviderFundingVersion(providerFundingVersion);
        }

        /// <summary>
        /// Gets provider funding based on prublished funding version
        /// </summary>
        /// <param name="publishedProviderVersion">Published Provider Version</param>
        /// <returns>Provider Version contents</returns>
        [HttpGet("{publishedProviderVersion}/fundings")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<dynamic>))]
        public async Task<IActionResult> GetFundings([FromRoute] string publishedProviderVersion)
        {
            return await _providerFundingVersionService.GetFundings(publishedProviderVersion);
        }
    }
}
