using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class ProviderProfileInformationController : ControllerBase
    {

        private readonly IProfileTotalsService _profileTotalsService;

        public ProviderProfileInformationController(
            IProfileTotalsService profileTotalsService
            )
        {
            Guard.ArgumentNotNull(profileTotalsService, nameof(profileTotalsService));

            _profileTotalsService = profileTotalsService;
        }

        /// <summary>
        /// Get latest profile totals for provider
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream Id</param>
        /// <param name="fundingPeriodId">Funding Period Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}/{providerId}/profileTotals")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProfileTotal>))]
        public async Task<IActionResult> GetLatestProfileTotalsForPublishedProvider([FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromRoute] string providerId)
        {
            return await _profileTotalsService.GetPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);
        }

        /// <summary>
        /// Get payment profiling totals for each released version of a provider.
        /// </summary>
        /// <param name="fundingStreamId">Funding stream Id</param>
        /// <param name="fundingPeriodId">Funding period Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <returns></returns>
        [HttpGet("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}/{providerId}/allProfileTotals")]
        [ProducesResponseType(200, Type = typeof(IDictionary<int, ProfilingVersion>))]
        [SwaggerOperation(Description = "Result is keyed by provider version ID. A new record is returned for each released version.")]
        public async Task<IActionResult> GetAllReleasedProfileTotalsForPublishedProvider([FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromRoute] string providerId)
        {
            return await _profileTotalsService.GetAllReleasedPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);
        }

        /// <summary>
        /// Get latest funding line profiles for provider for a funding stream
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <param name="fundingStreamId">Funding Stream Id</param>
        /// <param name="fundingLineId">Funding line id/code</param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(FundingLineProfile))]
        [ProducesResponseType(404)]
        [HttpGet("api/publishedproviderfundinglinedetails/{specificationId}/{providerId}/{fundingStreamId}/{fundingLineId}")]
        public async Task<IActionResult> GetFundingLinePublishedProviderDetails(
           [FromRoute] string specificationId,
           [FromRoute] string providerId,
           [FromRoute] string fundingStreamId,
           [FromRoute] string fundingLineId)
               => await _profileTotalsService.GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine
                   (specificationId, providerId, fundingStreamId, fundingLineId);

        /// <summary>
        /// Does the provider for a given funding line have a previous version in history
        /// </summary>
        /// <param name="specificationId"></param>
        /// <param name="providerId"></param>
        /// <param name="fundingStreamId"></param>
        /// <param name="fundingLineCode"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(bool))]
        [HttpGet("api/publishedproviderfundinglinedetails/{specificationId}/{providerId}/{fundingStreamId}/{fundingLineCode}/change-exists")]
        public async Task<IActionResult> PreviousProfileExistsForSpecificationForProviderForFundingLine(
            [FromRoute] string specificationId,
            [FromRoute] string providerId,
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingLineCode)
                => await _profileTotalsService.PreviousProfileExistsForSpecificationForProviderForFundingLine
                    (specificationId, providerId, fundingStreamId, fundingLineCode);

        /// <summary>
        /// Get previous historical profiling for a funding line for a provider in a funding stream
        /// </summary>
        /// <param name="specificationId">Specifcation Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <param name="fundingStreamId">Funding Stream Id</param>
        /// <param name="fundingLineCode">Funding line code</param>
        /// <returns></returns>
        [HttpGet("api/publishedproviderfundinglinedetails/{specificationId}/{providerId}/{fundingStreamId}/{fundingLineCode}/changes")]
        public async Task<IActionResult> GetPreviousProfilesForSpecificationForProviderForFundingLine(
            [FromRoute] string specificationId,
            [FromRoute] string providerId,
            [FromRoute] string fundingStreamId,
            [FromRoute] string fundingLineCode)
                => await _profileTotalsService.GetPreviousProfilesForSpecificationForProviderForFundingLine
                    (specificationId, providerId, fundingStreamId, fundingLineCode);

        /// <summary>
        /// Get profile history for a provider for all funding lines
        /// </summary>
        /// <param name="fundingStreamId">Funding Stream Id</param>
        /// <param name="fundingPeriodId">Funding period Id</param>
        /// <param name="providerId">Provider Id</param>
        /// <param name="profileHistoryService"></param>
        /// <returns></returns>
        [HttpGet("api/fundingstreams/{fundingStreamId}/fundingperiods/{fundingPeriodId}/providers/{providerId}/profilinghistory")]
        [ProducesResponseType(typeof(IEnumerable<PaymentFundingLineProfileTotals>), 200)]
        public async Task<IActionResult> GetProfileHistory([FromRoute] string fundingStreamId,
            [FromRoute] string fundingPeriodId,
            [FromRoute] string providerId,
            [FromServices] IProfileHistoryService profileHistoryService)
        {
            return await profileHistoryService.GetProfileHistory(fundingStreamId, fundingPeriodId, providerId);
        }
    }
}