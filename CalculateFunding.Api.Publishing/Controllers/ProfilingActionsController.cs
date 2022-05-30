using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class ProfilingActionsController : ControllerBase
    {
        private readonly IPublishedProviderProfilingService _publishedProviderProfilingService;
        private readonly ICustomProfileService _customProfileService;

        public ProfilingActionsController(
            IPublishedProviderProfilingService publishedProviderProfilingService,
            ICustomProfileService customProfileService)
        {
            Guard.ArgumentNotNull(publishedProviderProfilingService, nameof(publishedProviderProfilingService));
            Guard.ArgumentNotNull(customProfileService, nameof(customProfileService));

            _publishedProviderProfilingService = publishedProviderProfilingService;
            _customProfileService = customProfileService;
        }

        /// <summary>
        /// Assign rule based pattern to provider for funding line
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream Id</param>
        /// <param name="fundingPeriodId">Funding Period Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <param name="profilePatternKey">Profile pattern and funding line code</param>
        /// <returns></returns>
        [HttpPost("api/publishedprovider/fundingStream/{fundingStreamId}/fundingPeriod/{fundingPeriodId}/provider/{providerId}")]
        [ProducesResponseType(200, Type = typeof(HttpStatusCode))]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [SwaggerOperation(Description = "Change the rule based pattern used by the provider to the one specified. This will either be changing from an existing rule based pattern or a custom profile.")]
        public async Task<IActionResult> AssignProfilePatternKeyToPublishedProvider(
           [FromRoute] string fundingStreamId,
           [FromRoute] string fundingPeriodId,
           [FromRoute] string providerId,
           [FromBody] ProfilePatternKey profilePatternKey)
        {
            return await _publishedProviderProfilingService.AssignProfilePatternKey(
                fundingStreamId, fundingPeriodId, providerId, Request.GetCorrelationId(), profilePatternKey, Request.GetUserOrDefault());
        }

        /// <summary>
        /// Assign and set a custom profile for this provider for a funding line
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns></returns>
        [HttpPost("api/publishedproviders/customprofiles")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ApplyCustomProfilePattern(
            [FromBody] ApplyCustomProfileRequest request)
        {
            return await _customProfileService.ApplyCustomProfile(request, Request.GetUser(), Request.GetCorrelationId());
        }
    }
}
