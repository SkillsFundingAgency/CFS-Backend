using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Specs.Controllers
{
    public class SpecificationsController : Controller
    {
        private readonly ISpecificationsService _specService;
        private readonly ISpecificationsSearchService _specSearchService;
        private readonly IFundingService _fundingService;

        public SpecificationsController(
            ISpecificationsService specService,
            ISpecificationsSearchService specSearchService,
            IFundingService fundingService)
        {
            Guard.ArgumentNotNull(specService, nameof(specService));
            Guard.ArgumentNotNull(specSearchService, nameof(specSearchService));
            Guard.ArgumentNotNull(fundingService, nameof(fundingService));

            _specService = specService;
            _specSearchService = specSearchService;
            _fundingService = fundingService;
        }

        [Route("api/specs/specification-by-id")]
        [HttpGet]
        public async Task<IActionResult> RunGetSpecificationById()
        {
            return await _specService.GetSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summary-by-id")]
        [HttpGet]
        public async Task<IActionResult> RunGetSpecificationSummaryById()
        {
            return await _specService.GetSpecificationSummaryById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summaries-by-ids")]
        [HttpPost]
        public async Task<IActionResult> RunGetSpecificationSummariesByIds()
        {
            return await _specService.GetSpecificationSummariesByIds(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summaries")]
        [HttpGet]
        public async Task<IActionResult> RunGetSpecificationSummaries()
        {
            return await _specService.GetSpecificationSummaries(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-current-version-by-id")]
        [HttpGet]
        public async Task<IActionResult> RunGetCurrentSpecificationById()
        {
            return await _specService.GetCurrentSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpGet]
        public async Task<IActionResult> RunGetSpecifications()
        {
            return await _specService.GetSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-selected-for-funding")]
        [HttpGet]
        public async Task<IActionResult> RunGetSpecificationsSelectedForFunding()
        {
            return await _specService.GetSpecificationsSelectedForFunding(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-selected-for-funding-by-period")]
        [HttpGet]
        public async Task<IActionResult> RunGetSpecificationsSelectedForFundingByPeriod()
        {
            return await _specService.GetSpecificationsSelectedForFundingByPeriod(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/funding-streams-selected-for-funding-by-specification")]
        [HttpGet]
        public async Task<IActionResult> RunGetFundingStreamsSelectedForFundingBySpecification()
        {
            return await _specService.GetFundingStreamsSelectedForFundingBySpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-by-year")]
        [HttpGet]
        public async Task<IActionResult> RunSpecificationsByYear()
        {
            return await _specService.GetSpecificationsByFundingPeriodId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-by-name")]
        [HttpGet]
        public async Task<IActionResult> RunSpecificationByName()
        {
            return await _specService.GetSpecificationByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpPost]
        public async Task<IActionResult> RunSpecificationsCommands([FromBody]string value)
        {
            return await _specService.CreateSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-edit")]
        [HttpPut]
        public async Task<IActionResult> RunEditSpecification([FromBody]string value)
        {
            return await _specService.EditSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-edit-status")]
        [HttpPut]
        public async Task<IActionResult> RunEditSpecificationStatus([FromBody]string value)
        {
            return await _specService.EditSpecificationStatus(ControllerContext.HttpContext.Request);
        }

        [Obsolete("Moved to policy service")]
        [Route("api/specs/funding-streams")]
        [HttpGet]
        public async Task<IActionResult> RunFundingStreams()
        {
            return await _fundingService.GetFundingStreams();
        }

        [Route("api/specs/policy-by-name")]
        [HttpPost]
        public async Task<IActionResult> RunPolicyByName()
        {
            return await _specService.GetPolicyByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/policies")]
        [HttpPost]
        public async Task<IActionResult> RunCreatePolicy()
        {
            return await _specService.CreatePolicy(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/policies")]
        [HttpPut]
        public async Task<IActionResult> RunEditPolicy()
        {
            return await _specService.EditPolicy(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculation-by-name")]
        [HttpPost]
        public async Task<IActionResult> RunCalculationByName()
        {
            return await _specService.GetCalculationByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculation-by-id")]
        [HttpGet]
        public async Task<IActionResult> RunCalculationBySpecificationIdAndCalculationId()
        {
            return await _specService.GetCalculationBySpecificationIdAndCalculationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations-by-specificationid")]
        [HttpGet]
        public async Task<IActionResult> RunCalculationsBySpecificationId()
        {
            return await _specService.GetCalculationsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications/{specificationId}/baseline-calculations")]
        [HttpGet]
        public async Task<IActionResult> RunGetBaselineCalculations([FromRoute]string specificationId)
        {
            return await _specService.GetBaselineCalculations(specificationId, ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations")]
        [HttpPost]
        public async Task<IActionResult> RunCreateCalculation()
        {
            return await _specService.CreateCalculation(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations")]
        [HttpPut]
        public async Task<IActionResult> RunEditCalculation()
        {
            return await _specService.EditCalculation(ControllerContext.HttpContext.Request);
        }

        [Obsolete("Moved to policy service")]
        [Route("api/specs/get-fundingstreams")]
        [HttpGet]
        public async Task<IActionResult> RunGetFundingStreams()
        {
            return await _fundingService.GetFundingStreams();
        }

        [Obsolete("Moved to policy service")]
        [Route("api/specs/get-fundingstream-by-id")]
        [HttpGet]
        public async Task<IActionResult> RunGetFundingStreamById()
        {
            return await _fundingService.GetFundingStreamById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingstreams-for-specification")]
        [HttpGet]
        public async Task<IActionResult> RunGetFundingStreamsForSpecificationById()
        {
            return await _specService.GetFundingStreamsForSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/reindex")]
        [HttpGet]
        public async Task<IActionResult> ReIndex()
        {
            return await _specService.ReIndex();
        }

        [Route("api/specs/specifications-dataset-relationships-search")]
        [HttpPost]
        public async Task<IActionResult> RunSearchSpecificationsDatasetRelationships()
        {
            return await _specSearchService.SearchSpecificationDatasetRelationships(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-search")]
        [HttpPost]
        public async Task<IActionResult> RunSearchSpecifications()
        {
            return await _specSearchService.SearchSpecifications(ControllerContext.HttpContext.Request);
        }

        [Obsolete("Moved to policy service")]
        [Route("api/specs/save-fundingstream")]
        [HttpPost]
        public async Task<IActionResult> RunSaveFundingStreamn()
        {
            return await _fundingService.SaveFundingStream(ControllerContext.HttpContext.Request);
        }

        [Obsolete("Moved to policy service")]
        [Route("api/specs/get-fundingperiods")]
        [HttpGet]
        public async Task<IActionResult> RunGetFundingPeriods()
        {
            return await _fundingService.GetFundingPeriods(ControllerContext.HttpContext.Request);
        }

        [Obsolete("Moved to policy service")]
        [Route("api/specs/get-fundingperiod-by-id")]
        [HttpGet]
        public async Task<IActionResult> RunGetFundingPeriodById()
        {
            return await _fundingService.GetFundingPeriodById(ControllerContext.HttpContext.Request);
        }

        [Obsolete("Moved to policy service")]
        [Route("api/specs/save-fundingperiods")]
        [HttpPost]
        public async Task<IActionResult> RunSaveFundingPeriods()
        {
            return await _fundingService.SaveFundingPeriods(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-by-fundingperiod-and-fundingstream")]
        [HttpGet]
        public async Task<IActionResult> RunGetSpecificationsByFundingPeriodIdAndFundingStreamId()
        {
            return await _specService.GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/select-for-funding")]
        [HttpPost]
        public async Task<IActionResult> RunSelectSpecificationForFunding()
        {
            return await _specService.SelectSpecificationForFunding(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/check-publish-result-status")]
        [HttpPost]
        public async Task<IActionResult> RunCheckPublishResultStatus()
        {
            return await _specService.CheckPublishResultStatus(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/refresh-published-results")]
        [HttpPost]
        public async Task<IActionResult> RunRefreshPublishedResults()
        {
            return await _specService.RefreshPublishedResults(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/update-published-refreshed-date")]
        [HttpPost]
        public async Task<IActionResult> RunUpdatePublishedRefreshedDate()
        {
            return await _specService.UpdatePublishedRefreshedDate(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/update-calculation-last-updated-date")]
        [HttpPost]
        public async Task<IActionResult> RunUpdateCalculationLastUpdatedDate()
        {
            return await _specService.UpdateCalculationLastUpdatedDate(ControllerContext.HttpContext.Request);
        }
    }
}
