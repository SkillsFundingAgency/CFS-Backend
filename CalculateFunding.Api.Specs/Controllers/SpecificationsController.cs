using System.Threading.Tasks;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Specs.Controllers
{
    public class SpecificationsController : Controller
    {
        private readonly ISpecificationsService _specService;
        private readonly ISpecificationsSearchService _specSearchService;

        public SpecificationsController(
                    ISpecificationsService specService,
                    ISpecificationsSearchService specSearchService)
        {
            Guard.ArgumentNotNull(specService, nameof(specService));
            Guard.ArgumentNotNull(specSearchService, nameof(specSearchService));

            _specService = specService;
            _specSearchService = specSearchService;
        }

        [Route("api/specs/specification-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecificationById()
        {
            return _specService.GetSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summary-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecificationSummaryById()
        {
            return _specService.GetSpecificationSummaryById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summaries-by-ids")]
        [HttpPost]
        public Task<IActionResult> RunGetSpecificationSummariesByIds()
        {
            return _specService.GetSpecificationSummariesByIds(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summaries")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecificationSummaries()
        {
            return _specService.GetSpecificationSummaries(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-current-version-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetCurrentSpecificationById()
        {
            return _specService.GetCurrentSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecifications()
        {
            return _specService.GetSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-selected-for-funding")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecificationsSelectedForFunding()
        {
            return _specService.GetSpecificationsSelectedForFunding(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-by-year")]
        [HttpGet]
        public Task<IActionResult> RunSpecificationsByYear()
        {
            return _specService.GetSpecificationsByFundingPeriodId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-by-name")]
        [HttpGet]
        public Task<IActionResult> RunSpecificationByName()
        {
            return _specService.GetSpecificationByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpPost]
        public Task<IActionResult> RunSpecificationsCommands([FromBody]string value)
        {
            return _specService.CreateSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-edit")]
        [HttpPut]
        public Task<IActionResult> RunEditSpecification([FromBody]string value)
        {
            return _specService.EditSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-edit-status")]
        [HttpPut]
        public Task<IActionResult> RunEditSpecificationStatus([FromBody]string value)
        {
            return _specService.EditSpecificationStatus(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/funding-streams")]
        [HttpGet]
        public Task<IActionResult> RunFundingStreams()
        {
            return _specService.GetFundingStreams(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/policy-by-name")]
        [HttpPost]
        public Task<IActionResult> RunPolicyByName()
        {
            return _specService.GetPolicyByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/policies")]
        [HttpPost]
        public Task<IActionResult> RunCreatePolicy()
        {
            return _specService.CreatePolicy(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/policies")]
        [HttpPut]
        public Task<IActionResult> RunEditPolicy()
        {
            return _specService.EditPolicy(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculation-by-name")]
        [HttpPost]
        public Task<IActionResult> RunCalculationByName()
        {
            return _specService.GetCalculationByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculation-by-id")]
        [HttpGet]
        public Task<IActionResult> RunCalculationBySpecificationIdAndCalculationId()
        {
            return _specService.GetCalculationBySpecificationIdAndCalculationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations-by-specificationid")]
        [HttpGet]
        public Task<IActionResult> RunCalculationsBySpecificationId()
        {
            return _specService.GetCalculationsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations")]
        [HttpPost]
        public Task<IActionResult> RunCreateCalculation()
        {
            return _specService.CreateCalculation(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations")]
        [HttpPut]
        public Task<IActionResult> RunEditCalculation()
        {
            return _specService.EditCalculation(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingstreams")]
        [HttpGet]
        public Task<IActionResult> RunGetFundingStreams()
        {
            return _specService.GetFundingStreams(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingstream-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetFundingStreamById()
        {
            return _specService.GetFundingStreamById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingstreams-for-specification")]
        [HttpGet]
        public Task<IActionResult> RunGetFundingStreamsForSpecificationById()
        {
            return _specService.GetFundingStreamsForSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/reindex")]
        [HttpGet]
        public Task<IActionResult> ReIndex()
        {
            return _specService.ReIndex();
        }

        [Route("api/specs/specifications-dataset-relationships-search")]
        [HttpPost]
        public Task<IActionResult> RunSearchSpecificationsDatasetRelationships()
        {
            return _specSearchService.SearchSpecificationDatasetRelationships(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-search")]
        [HttpPost]
        public Task<IActionResult> RunSearchSpecifications()
        {
            return _specSearchService.SearchSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/save-fundingstream")]
        [HttpPost]
        public Task<IActionResult> RunSaveFundingStreamn()
        {
            return _specService.SaveFundingStream(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingperiods")]
        [HttpGet]
        public Task<IActionResult> RunGetFundingPeriods()
        {
            return _specService.GetFundingPeriods(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/save-fundingperiods")]
        [HttpPost]
        public Task<IActionResult> RunSaveFundingPeriods()
        {
            return _specService.SaveFundingPeriods(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-by-fundingperiod-and-fundingstream")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecificationsByFundingPeriodIdAndFundingStreamId()
        {
            return _specService.GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/select-for-funding")]
        [HttpPost]
        public Task<IActionResult> RunSelectSpecificationForFunding()
        {
            return _specService.SelectSpecificationForFunding(ControllerContext.HttpContext.Request);
        }
    }
}
