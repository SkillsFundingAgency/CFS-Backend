using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{

    public class SpecificationsController : BaseController
    {
        private readonly ISpecificationsService _specService;
        private readonly ISpecificationsSearchService _specSearchService;

        public SpecificationsController(ISpecificationsService specService, 
           ISpecificationsSearchService specSearchService,  IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _specService = specService;
            _specSearchService = specSearchService;
        }

        [Route("api/specs/specification-by-id")]
        [HttpGet]
        public Task<IActionResult>RunGetSpecificationById()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summary-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecificationSummaryById()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationSummaryById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summaries-by-ids")]
        [HttpPost]
        public Task<IActionResult> RunGetSpecificationSummariesByIds()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationSummariesByIds(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summaries")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecificationSummaries()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationSummaries(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-current-version-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetCurrentSpecificationById()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetCurrentSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpGet]
        public Task<IActionResult> RunGetSpecifications()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-by-year")]
        [HttpGet]
        public Task<IActionResult> RunSpecificationsByYear()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationsByFundingPeriodId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-by-name")]
        [HttpGet]
        public Task<IActionResult> RunSpecificationByName()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpPost]
        public Task<IActionResult> RunSpecificationsCommands([FromBody]string value)
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.CreateSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-edit")]
        [HttpPut]
        public Task<IActionResult> RunEditSpecification([FromBody]string value)
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.EditSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-edit-status")]
        [HttpPut]
        public Task<IActionResult> RunEditSpecificationStatus([FromBody]string value)
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.EditSpecificationStatus(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/funding-streams")]
        [HttpGet]
        public Task<IActionResult> RunFundingStreams()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetFundingStreams(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/policy-by-name")]
        [HttpPost]
        public Task<IActionResult> RunPolicyByName()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

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
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

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
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.CreateCalculation(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations")]
        [HttpPut]
        public Task<IActionResult> RunEditCalculation()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.EditCalculation(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingstreams")]
        [HttpGet]
        public Task<IActionResult> RunGetFundingStreams()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetFundingStreams(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingstream-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetFundingStreamById()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetFundingStreamById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingstreams-for-specification")]
        [HttpGet]
        public Task<IActionResult> RunGetFundingStreamsForSpecificationById()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetFundingStreamsForSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/reindex")]
        [HttpGet]
        public Task<IActionResult> ReIndex()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.ReIndex();
        }

        [Route("api/specs/specifications-dataset-relationships-search")]
        [HttpPost]
        public Task<IActionResult> RunSearchSpecificationsDatasetRelationships()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specSearchService.SearchSpecificationDatasetRelationships(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-search")]
        [HttpPost]
        public Task<IActionResult> RunSearchSpecifications()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specSearchService.SearchSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/save-fundingstream")]
        [HttpPost]
        public Task<IActionResult> RunSaveFundingStreamn()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.SaveFundingStream(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/get-fundingperiods")]
        [HttpGet]
        public Task<IActionResult> RunGetFundingPeriods()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetFundingPeriods(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/save-fundingperiods")]
        [HttpPost]
        public Task<IActionResult> RunSaveFundingPeriods()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.SaveFundingPeriods(ControllerContext.HttpContext.Request);
        }
    }
}
