using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class ProviderByFundingStreamController : ControllerBase
    {
        private readonly IFundingStreamProviderVersionService _fundingStreamProviderVersionService;

        public ProviderByFundingStreamController(IFundingStreamProviderVersionService fundingStreamProviderVersionService)
        {
            Guard.ArgumentNotNull(fundingStreamProviderVersionService, nameof(fundingStreamProviderVersionService));

            _fundingStreamProviderVersionService = fundingStreamProviderVersionService;
        }

        /// <summary>
        ///     Fetches all providers as at the current provider version for the funding stream
        /// </summary>
        /// <param name="fundingStreamId">the funding stream to get all current providers for</param>
        /// <returns>ProviderVersion[]</returns>
        [HttpGet("api/providers/fundingstreams/{fundingStreamId}/current")]
        [ProducesResponseType(200, Type = typeof(ProviderVersion))]
        public async Task<IActionResult> GetCurrentProvidersForFundingStream([FromRoute] string fundingStreamId) =>
            await _fundingStreamProviderVersionService.GetCurrentProvidersForFundingStream(fundingStreamId);

        /// <summary>
        ///     Fetches latest version of provider metadata for funding stream
        /// </summary>
        /// <param name="fundingStreamId">the funding stream to get current provider metadata for</param>
        /// <returns>ProviderVersion[]</returns>
        [HttpGet("api/providers/fundingstreams/{fundingStreamId}/current/metadata")]
        [ProducesResponseType(200, Type = typeof(CurrentProviderVersionMetadata))]
        public async Task<IActionResult> GetCurrentProviderMetadataForFundingStream([FromRoute] string fundingStreamId) =>
            await _fundingStreamProviderVersionService.GetCurrentProviderMetadataForFundingStream(fundingStreamId);

        /// <summary>
        ///     Fetches latest version of provider metadata for all funding streams
        /// </summary>
        /// <returns>CurrentProviderVersionMetadata[]</returns>
        [HttpGet("api/providers/fundingstreams")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<CurrentProviderVersionMetadata>))]
        public async Task<IActionResult> GetCurrentProviderMetadataForAllFundingStreams() =>
            await _fundingStreamProviderVersionService.GetCurrentProviderMetadataForAllFundingStreams();

        /// <summary>
        ///     Sets the current provider version id for the funding stream
        /// </summary>
        /// <param name="fundingStreamId">the funding stream to update</param>
        /// <param name="providerVersionId">the new Current provider version id</param>
        /// <param name="providerSnapshotId">ProviderSnapshotId (optional)</param>
        [HttpPut("api/providers/fundingstreams/{fundingStreamId}/current/{providerVersionId}")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SetCurrentProviderVersion([FromRoute] string fundingStreamId,
            [FromRoute] string providerVersionId, [FromQuery] int? providerSnapshotId) =>
            await _fundingStreamProviderVersionService.SetCurrentProviderVersionForFundingStream(fundingStreamId,
                providerVersionId, providerSnapshotId);

        /// <summary>
        ///     Gets the version of the provider as at the Current
        ///     provider version for the funding stream
        /// </summary>
        /// <param name="fundingStreamId">the funding stream to get the current provider from</param>
        /// <param name="providerId">the provider to locate</param>
        /// <returns>ProviderVersionSearchResult</returns>
        [HttpGet("api/providers/{providerId}/fundingstreams/{fundingStreamId}/current")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResult))]
        public async Task<IActionResult> GetProviderByIdFromFundingStreamCurrentProviderVersion([FromRoute] string fundingStreamId,
            [FromRoute] string providerId) =>
            await _fundingStreamProviderVersionService.GetCurrentProviderForFundingStream(fundingStreamId,
                providerId);

        /// <summary>
        ///     Searches across all of the providers in the Current
        ///     provider version id for the funding stream
        /// </summary>
        /// <param name="fundingStreamId">the funding stream to get the current provider version from</param>
        /// <param name="search">the search parameters</param>
        /// <returns>ProviderVersionSearchResults</returns>
        [HttpPost("api/providers/fundingstreams/{fundingStreamId}/current/search")]
        [ProducesResponseType(200, Type = typeof(ProviderVersionSearchResults))]
        public async Task<IActionResult> SearchFundingStreamCurrentProviders([FromRoute] string fundingStreamId,
            [FromBody] SearchModel search) =>
            await _fundingStreamProviderVersionService.SearchCurrentProviderVersionsForFundingStream(fundingStreamId,
                search);
    }
}