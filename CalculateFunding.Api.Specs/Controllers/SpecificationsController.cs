using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search.Results;
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
        [Produces(typeof(Specification))]
        public async Task<IActionResult> GetSpecificationById()
        {
            return await _specService.GetSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summary-by-id")]
        [HttpGet]
        [Produces(typeof(SpecificationSummary))]
        public async Task<IActionResult> GetSpecificationSummaryById()
        {
            return await _specService.GetSpecificationSummaryById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summaries-by-ids")]
        [HttpPost]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationSummariesByIds()
        {
            return await _specService.GetSpecificationSummariesByIds(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-summaries")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationSummaries()
        {
            return await _specService.GetSpecificationSummaries(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Specification>))]
        public async Task<IActionResult> GetSpecifications()
        {
            return await _specService.GetSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-selected-for-funding")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Specification>))]
        public async Task<IActionResult> GetSpecificationsSelectedForFunding()
        {
            return await _specService.GetSpecificationsSelectedForFunding(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-selected-for-funding-by-period")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationsSelectedForFundingByPeriod()
        {
            return await _specService.GetSpecificationsSelectedForFundingByPeriod(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/funding-streams-selected-for-funding-by-specification")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Reference>))]
        public async Task<IActionResult> GetFundingStreamsSelectedForFundingBySpecification()
        {
            return await _specService.GetFundingStreamsSelectedForFundingBySpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-by-year")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> SpecificationsByYear()
        {
            return await _specService.GetSpecificationsByFundingPeriodId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-by-name")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Specification>))]
        public async Task<IActionResult> SpecificationByName()
        {
            return await _specService.GetSpecificationByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpPost]
        [Produces(typeof(SpecificationSummary))]
        public async Task<IActionResult> SpecificationsCommands([FromBody]string value)
        {
            return await _specService.CreateSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-edit")]
        [HttpPut]
        [Produces(typeof(Specification))]
        public async Task<IActionResult> EditSpecification([FromBody]string value)
        {
            return await _specService.EditSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-edit-status")]
        [HttpPut]
        [Produces(typeof(PublishStatusResultModel))]
        public async Task<IActionResult> EditSpecificationStatus([FromBody]string value)
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
        [Produces(typeof(SpecificationDatasetRelationshipsSearchResults))]
        public async Task<IActionResult> SearchSpecificationsDatasetRelationships()
        {
            return await _specSearchService.SearchSpecificationDatasetRelationships(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-search")]
        [HttpPost]
        [Produces(typeof(SpecificationSearchResults))]
        public async Task<IActionResult> SearchSpecifications()
        {
            return await _specSearchService.SearchSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-by-fundingperiod-and-fundingstream")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationsByFundingPeriodIdAndFundingStreamId()
        {
            return await _specService.GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/fundingstream-ids-for-funding-specifications")]
        [HttpGet]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<IActionResult> RunGetFundingStreamIdsForFundingSpecifications()
        {
            return await _specService.GetFundingStreamIdsForSelectedFundingSpecifications();
        }

        [Route("api/specs/select-for-funding")]
        [HttpPost]
        public async Task<IActionResult> RunSelectSpecificationForFunding()
        {
            return await _specService.SelectSpecificationForFunding(ControllerContext.HttpContext.Request);
        }
        
        [HttpPost("api/specs/deselect-for-funding/{specificationId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeselectSpecificationForFunding([FromRoute] string specificationId)
        {
            return await _specService.DeselectSpecificationForFunding(specificationId);
        }

        [Route("api/specs/{specificationId}/templates/{fundingStreamId}/")]
        [HttpPut]
        [Produces(typeof(HttpStatusCode))]
        public async Task<IActionResult> RunSetAssignedTemplateVersion([FromRoute]string specificationId, [FromRoute]string fundingStreamId, [FromBody]string templateVersion)
        {
            return await _specService.SetAssignedTemplateVersion(specificationId, fundingStreamId, templateVersion);
        }

        [Route("/api/specs/{specificationId}/publishdates")]
        [HttpGet]
        [Produces(typeof(SpecificationPublishDateModel))]
        public async Task<IActionResult> GetPublishDates([FromRoute]string specificationId)
        {
            return await _specService.GetPublishDates(specificationId);
        }

        [Route("/api/specs/{specificationId}/profilevariationpointers")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationProfileVariationPointerModel>))]
        public async Task<IActionResult> GetProfileVariationPointers([FromRoute]string specificationId)
        {
            return await _specService.GetProfileVariationPointers(specificationId);
        }

        [Route("/api/specs/{specificationId}/publishdates")]
        [HttpPut]
        [Produces(typeof(HttpStatusCode))]
        public async Task<IActionResult> SetPublishDates([FromRoute]string specificationId,
           [FromBody]SpecificationPublishDateModel specificationPublishDateModel)
        {
            return await _specService.SetPublishDates(specificationId, specificationPublishDateModel);
        }

        [Route("/api/specs/{specificationId}/profilevariationpointers")]
        [HttpPut]
        [Produces(typeof(HttpStatusCode))]
        public async Task<IActionResult> SetProfileVariationPointers([FromRoute]string specificationId,
           [FromBody]IEnumerable<SpecificationProfileVariationPointerModel> specificationProfileVariationPointerModels)
        {
            return await _specService.SetProfileVariationPointers(specificationId, specificationProfileVariationPointerModels);
        }

        [Route("/api/specs/{specificationId}/profilevariationpointer")]
        [HttpPut]
        [Produces(typeof(HttpStatusCode))]
        public async Task<IActionResult> SetProfileVariationPointer([FromRoute]string specificationId,
           [FromBody]SpecificationProfileVariationPointerModel specificationProfileVariationPointerModel)
        {
            return await _specService.SetProfileVariationPointer(specificationId, specificationProfileVariationPointerModel);
        }

        [Route("api/specs/fundingperiods-by-fundingstream-id/{fundingStreamId}")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Reference>))]
        public async Task<IActionResult> GetFundingPeriodsByFundingStreamIds([FromRoute]string fundingStreamId)
        {
            return await _specService.GetFundingPeriodsByFundingStreamIdsForSelectedSpecifications(fundingStreamId);
        }

        [Route("api/specs/{specificationId}")]
        [HttpDelete]
        public async Task<IActionResult> SoftDeleteSpecificationById()
        {
            return await _specService.SoftDeleteSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/{specificationId}/permanent")]
        [HttpDelete]
        public async Task<IActionResult> PermanentDeleteSpecificationById()
        {
            return await _specService.PermanentDeleteSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/fundingstream-id-for-specifications")]
        [HttpGet]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetDistinctFundingStreamsForSpecifications()
        {
            return await _specService.GetDistinctFundingStreamsForSpecifications();
        }
    }
}
