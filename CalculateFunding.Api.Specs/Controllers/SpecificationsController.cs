using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace CalculateFunding.Api.Specs.Controllers
{
    public class SpecificationsController : ControllerBase
    {
        private readonly ISpecificationsService _specService;
        private readonly ISpecificationsSearchService _specSearchService;
        private readonly ISpecificationsReportService _specificationsReportService;
        private readonly ISpecificationIndexingService _specificationIndexingService;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public SpecificationsController(
            ISpecificationsService specService,
            ISpecificationsSearchService specSearchService,
            IWebHostEnvironment hostingEnvironment,
            ISpecificationsReportService specificationsReportService,
            ISpecificationIndexingService specificationIndexingService)
        {
            Guard.ArgumentNotNull(specService, nameof(specService));
            Guard.ArgumentNotNull(specSearchService, nameof(specSearchService));
            Guard.ArgumentNotNull(hostingEnvironment, nameof(hostingEnvironment));
            Guard.ArgumentNotNull(specificationsReportService, nameof(specificationsReportService));
            Guard.ArgumentNotNull(specificationIndexingService, nameof(specificationIndexingService));

            _specService = specService;
            _specSearchService = specSearchService;
            _hostingEnvironment = hostingEnvironment;
            _specificationsReportService = specificationsReportService;
            _specificationIndexingService = specificationIndexingService;
        }

        [Route("api/specs/specification-by-id")]
        [HttpGet]
        [Produces(typeof(Specification))]
        public async Task<IActionResult> GetSpecificationById([FromQuery] string specificationId)
        {
            return await _specService.GetSpecificationById(specificationId);
        }

        [Route("api/specs/specification-summary-by-id")]
        [HttpGet]
        [Produces(typeof(SpecificationSummary))]
        public async Task<IActionResult> GetSpecificationSummaryById([FromQuery] string specificationId)
        {
            return await _specService.GetSpecificationSummaryById(specificationId);
        }

        [Route("api/specs/specification-summaries-by-ids")]
        [HttpPost]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationSummariesByIds([FromBody] string[] specificationIds)
        {
            return await _specService.GetSpecificationSummariesByIds(specificationIds);
        }

        [Route("api/specs/specification-summaries")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationSummaries()
        {
            return await _specService.GetSpecificationSummaries();
        }

        [Route("api/specs/specifications")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Specification>))]
        public async Task<IActionResult> GetSpecifications()
        {
            return await _specService.GetSpecifications();
        }

        [Route("api/specs/specifications-selected-for-funding")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Specification>))]
        public async Task<IActionResult> GetSpecificationsSelectedForFunding()
        {
            return await _specService.GetSpecificationsSelectedForFunding();
        }

        [Route("api/specs/specifications-selected-for-funding-by-period")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationsSelectedForFundingByPeriod([FromQuery] string fundingPeriodId)
        {
            return await _specService.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId);
        }

        [Route("api/specs/funding-streams-selected-for-funding-by-specification")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Reference>))]
        public async Task<IActionResult> GetFundingStreamsSelectedForFundingBySpecification([FromQuery] string specificationId)
        {
            return await _specService.GetFundingStreamsSelectedForFundingBySpecification(specificationId);
        }

        [Route("api/specs/specifications-by-year")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> SpecificationsByYear([FromQuery] string fundingPeriodId)
        {
            return await _specService.GetSpecificationsByFundingPeriodId(fundingPeriodId);
        }

        [Route("api/specs/specification-by-name")]
        [HttpGet]
        [Produces(typeof(IEnumerable<Specification>))]
        public async Task<IActionResult> SpecificationByName([FromQuery] string specificationName)
        {
            return await _specService.GetSpecificationByName(specificationName);
        }

        [Route("api/specs/specifications")]
        [HttpPost]
        [Produces(typeof(SpecificationSummary))]
        public async Task<IActionResult> SpecificationsCommands([FromBody]SpecificationCreateModel specificationCreateModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUser();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return await _specService.CreateSpecification(specificationCreateModel, user, correlationId);
        }

        [Route("api/specs/specification-edit")]
        [HttpPut]
        [Produces(typeof(Specification))]
        public async Task<IActionResult> EditSpecification([FromQuery] string specificationId, [FromBody] SpecificationEditModel specificationEditModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUser();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return await _specService.EditSpecification(specificationId, specificationEditModel, user, correlationId);
        }

        [Route("api/specs/specification-edit-status")]
        [HttpPut]
        [Produces(typeof(PublishStatusResultModel))]
        public async Task<IActionResult> EditSpecificationStatus([FromQuery] string specificationId, [FromBody] EditStatusModel editStatusModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUser();

            return await _specService.EditSpecificationStatus(specificationId, editStatusModel, user);
        }

        [Route("api/specs/reindex")]
        [HttpGet]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ReIndex()
        {
            return await _specService.ReIndex();
        }

        [Route("api/specs/specifications-dataset-relationships-search")]
        [HttpPost]
        [Produces(typeof(SpecificationDatasetRelationshipsSearchResults))]
        public async Task<IActionResult> SearchSpecificationsDatasetRelationships([FromBody] SearchModel searchModel)
        {
            return await _specSearchService.SearchSpecificationDatasetRelationships(searchModel);
        }

        [Route("api/specs/specifications-search")]
        [HttpPost]
        [Produces(typeof(SpecificationSearchResults))]
        public async Task<IActionResult> SearchSpecifications([FromBody] SearchModel searchModel)
        {
            return await _specSearchService.SearchSpecifications(searchModel);
        }

        [Route("api/specs/specifications-by-fundingperiod-and-fundingstream/{fundingStreamId}/{fundingPeriodId}")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationsByFundingPeriodIdAndFundingStreamId(
            [FromRoute] string fundingPeriodId, [FromRoute] string fundingStreamId)
        {
            return await _specService
                .GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(fundingPeriodId, fundingStreamId);
        }

        [Route("api/specs/specifications-by-fundingperiod-and-fundingstream/{fundingStreamId}/{fundingPeriodId}/with-results")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSpecificationResultsByFundingPeriodIdAndFundingStreamId(
            [FromRoute] string fundingPeriodId, [FromRoute] string fundingStreamId)
        {
            return await _specService
                .GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId(fundingPeriodId, fundingStreamId);
        }

        [Route("api/specs/specifications-by-fundingperiod-and-fundingstream/{fundingStreamId}/{fundingPeriodId}/approved")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId(
            [FromRoute] string fundingPeriodId, [FromRoute] string fundingStreamId)
        {
            return await _specService
                .GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId(fundingPeriodId, fundingStreamId);
        }

        [Route("api/specs/specifications-by-fundingperiod-and-fundingstream/{fundingStreamId}/{fundingPeriodId}/selected")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationSummary>))]
        public async Task<IActionResult> GetSelectedSpecificationsByFundingPeriodIdAndFundingStreamId(
    [FromRoute] string fundingPeriodId, [FromRoute] string fundingStreamId)
        {
            return await _specService
                .GetSelectedSpecificationsByFundingPeriodIdAndFundingStreamId(fundingPeriodId, fundingStreamId);
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
        [ProducesResponseType(204)]
        public async Task<IActionResult> RunSelectSpecificationForFunding([FromQuery] string specificationId)
        {
            return await _specService.SelectSpecificationForFunding(specificationId);
        }
        
        [HttpPost("api/specs/deselect-for-funding/{specificationId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(204)]
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
        [Produces(typeof(bool))]
        public async Task<IActionResult> SoftDeleteSpecificationById([FromRoute] string specificationId)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUser();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return await _specService.SoftDeleteSpecificationById(specificationId, user, correlationId);
        }

        [Route("api/specs/{specificationId}/permanent")]
        [HttpDelete]
        [Produces(typeof(bool))]
        public async Task<IActionResult> PermanentDeleteSpecificationById([FromRoute] string specificationId)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUser();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            // only allow permenant delete if in development
            return await _specService.PermanentDeleteSpecificationById(specificationId, user, correlationId, _hostingEnvironment.IsDevelopment());
        }

        [Route("api/specs/fundingstream-id-for-specifications")]
        [HttpGet]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetDistinctFundingStreamsForSpecifications()
        {
            return await _specService.GetDistinctFundingStreamsForSpecifications();
        }

        [Route("api/specs/{specificationId}/report-metadata")]
        [HttpGet]
        [Produces(typeof(IEnumerable<SpecificationReport>))]
        public IActionResult GetReportMetadataForSpecifications([FromRoute] string specificationId)
        {
            return _specificationsReportService.GetReportMetadata(specificationId);
        }

        [Route("api/specs/download-report/{reportId}")]
        [HttpGet]
        [Produces(typeof(SpecificationsDownloadModel))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DownloadSpecificationReport([FromRoute] string reportId)
        {
            return await _specificationsReportService.DownloadReport(reportId);
        }
        
        [HttpPost("api/specs/{specificationId}/reindex")]
        [Produces(typeof(JobViewModel))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ReIndexSpecification([FromRoute] string specificationId)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUser();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return await _specificationIndexingService.QueueSpecificationIndexJob(specificationId,
                user,
                correlationId);
        }
    }
}
