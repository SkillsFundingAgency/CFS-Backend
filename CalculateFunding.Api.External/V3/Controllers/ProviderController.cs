using CalculateFunding.Api.External.V3.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/providers")]
    public class ProviderController : ControllerBase
    {
        private readonly IPublishedProviderRetrievalService _providerServiceRetrievalService;

        public ProviderController(IPublishedProviderRetrievalService providerServiceRetrievalService)
        {
            _providerServiceRetrievalService = providerServiceRetrievalService;
        }

        [HttpGet("{publishedProviderVersion}")]
        [ProducesResponseType(200, Type = typeof(Models.ProviderVersionSearchResult))]
        public async Task<IActionResult> GetPublishedProviderInformation([FromRoute] string publishedProviderVersion)
        {
            return await _providerServiceRetrievalService.GetPublishedProviderInformation(publishedProviderVersion);
        }
    }
}
