using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
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

        [Route("api/specs/{specificationId}/templates/{fundingStreamId}/")]
        [HttpPut]
        public async Task<IActionResult> RunSetAssignedTemplateVersion([FromRoute]string specificationId, [FromRoute]string fundingStreamId, [FromBody]string templateVersion)
        {
            return await _specService.SetAssignedTemplateVersion(specificationId, fundingStreamId, templateVersion);
        }

        [Route("/api/specs/{specificationId}/publishdates")]
        [HttpGet]
        public async Task<IActionResult> GetPublishDates([FromRoute]string specificationId)
        {
            return await _specService.GetPublishDates(specificationId);
        }


        [Route("/api/specs/{specificationId}/publishdates")]
        [HttpPut]
        public async Task<IActionResult> SetPublishDates([FromRoute]string specificationId,
           [FromBody]SpecificationPublishDateModel specificationPublishDateModel)
        {
            return await _specService.SetPublishDates(specificationId, specificationPublishDateModel);
        }

    }
}
