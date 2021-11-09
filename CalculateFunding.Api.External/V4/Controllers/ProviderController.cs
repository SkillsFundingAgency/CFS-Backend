using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiController]
    [ApiVersion("4.0")]
    [Route("api/v{version:apiVersion}/{channel}/providers")]
    public class ProviderController : ControllerBase
    {
        private readonly IPublishedProviderRetrievalService _providerServiceRetrievalService;

        public ProviderController(IPublishedProviderRetrievalService providerServiceRetrievalService)
        {
            _providerServiceRetrievalService = providerServiceRetrievalService;
        }

        [HttpGet("{publishedProviderVersion}")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResult))]
        public async Task<ActionResult<ProviderVersionSearchResult>> GetPublishedProviderInformation(
            [FromRoute] string channel,
            [FromRoute] string publishedProviderVersion)
        {
            return await _providerServiceRetrievalService.GetPublishedProviderInformation(channel, publishedProviderVersion);
        }
    }
}
