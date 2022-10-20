using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Calcs.Controllers
{
    public class CalculationsController : ControllerBase
    {
        private readonly ICalculationFundingLineQueryService _calculationFundingLineQueryService;
        private readonly ICalculationService _calcsService;
        private readonly IReferencedSpecificationReMapService _referencedSpecificationReMapService;
        private readonly IPreviewService _previewService;
        private readonly ICalculationsSearchService _calcsSearchService;
        private readonly IBuildProjectsService _buildProjectsService;
        private readonly IQueueReIndexSpecificationCalculationRelationships _calculationRelationships;
        private readonly ICodeContextCache _codeContextCache;

        public CalculationsController(
            ICalculationService calcsService,
            ICalculationsSearchService calcsSearchService,
            IPreviewService previewService,
            IBuildProjectsService buildProjectsService,
            IReferencedSpecificationReMapService referencedSpecificationReMapService,
            IQueueReIndexSpecificationCalculationRelationships calculationRelationships,
            ICalculationFundingLineQueryService calculationFundingLineQueryService,
            ICodeContextCache codeContextCache)
        {
            Guard.ArgumentNotNull(calcsService, nameof(calcsService));
            Guard.ArgumentNotNull(calcsSearchService, nameof(calcsSearchService));
            Guard.ArgumentNotNull(previewService, nameof(previewService));
            Guard.ArgumentNotNull(buildProjectsService, nameof(buildProjectsService));
            Guard.ArgumentNotNull(referencedSpecificationReMapService, nameof(referencedSpecificationReMapService));
            Guard.ArgumentNotNull(calculationRelationships, nameof(calculationRelationships));
            Guard.ArgumentNotNull(calculationFundingLineQueryService, nameof(calculationFundingLineQueryService));
            Guard.ArgumentNotNull(codeContextCache, nameof(codeContextCache));

            _calcsService = calcsService;
            _previewService = previewService;
            _calcsSearchService = calcsSearchService;
            _buildProjectsService = buildProjectsService;
            _referencedSpecificationReMapService = referencedSpecificationReMapService;
            _calculationRelationships = calculationRelationships;
            _calculationFundingLineQueryService = calculationFundingLineQueryService;
            _codeContextCache = codeContextCache;
        }

        [HttpGet("api/specifications/{specificationId}/calculations/calculationType/{calculationType}")]
        [Produces(typeof(CalculationSearchResults))]
        public async Task<IActionResult> SearchCalculationsForSpecification([FromRoute] string specificationId,
            [FromRoute] CalculationType calculationType,
            [FromQuery] PublishStatus? status,
            [FromQuery] string searchTerm,
            [FromQuery] int? page) =>
            await _calcsSearchService.SearchCalculations(specificationId, calculationType, status, searchTerm, page);

        [Route("api/calcs/calculations-search")]
        [HttpPost]
        [Produces(typeof(CalculationSearchResults))]
        public Task<IActionResult> CalculationsSearch([FromBody] SearchModel searchModel) => _calcsSearchService.SearchCalculations(searchModel);

        [Obsolete("Migrate to REST method")]
        [Route("api/calcs/calculation-by-id")]
        [HttpGet]
        [Produces(typeof(CalculationResponseModel))]
        public Task<IActionResult> CalculationById([FromQuery] string calculationId) => _calcsService.GetCalculationById(calculationId);

        [Route("api/calcs/calculations/by-id/{calculationId}")]
        [HttpGet]
        [Produces(typeof(CalculationResponseModel))]
        public Task<IActionResult> GetCalculationById([FromRoute] string calculationId) => _calcsService.GetCalculationById(calculationId);

        [Route("api/calcs/calculation-summaries-for-specification")]
        [HttpGet]
        [Produces(typeof(IEnumerable<CalculationSummaryModel>))]
        public Task<IActionResult> GetCalculationSummariesForSpecification([FromQuery] string specificationId) => _calcsService.GetCalculationSummariesForSpecification(specificationId);

        [Route("api/calcs/current-calculations-for-specification")]
        [HttpGet]
        [Produces(typeof(IEnumerable<CalculationResponseModel>))]
        public Task<IActionResult> GetCurrentCalculationsForSpecification([FromQuery] string specificationId) => _calcsService.GetCurrentCalculationsForSpecification(specificationId);

        [Route("api/calcs/specifications/{specificationId}/calculations/{calculationId}")]
        [HttpPut]
        [Produces(typeof(CalculationResponseModel))]
        public Task<IActionResult> EditCalculation([FromRoute] string specificationId,
            [FromRoute] string calculationId,
            [FromBody] CalculationEditModel model)
        {
            HttpRequest httpRequest = ControllerContext.HttpContext.Request;

            return _calcsService.EditCalculation(
                specificationId,
                calculationId,
                model,
                httpRequest.GetUser(),
                httpRequest.GetCorrelationId(),
                calculationEditMode: CalculationEditMode.User);
        }

        [Route("api/calcs/specifications/{specificationId}/calculations/{calculationId}/{skipInstruct}")]
        [HttpPut]
        [Produces(typeof(CalculationResponseModel))]
        public Task<IActionResult> EditCalculation([FromRoute] string specificationId,
            [FromRoute] string calculationId,
            [FromRoute] bool skipInstruct,
            [FromBody] CalculationEditModel model)
        {
            HttpRequest httpRequest = ControllerContext.HttpContext.Request;

            return _calcsService.EditCalculation(
                specificationId,
                calculationId,
                model,
                httpRequest.GetUser(),
                httpRequest.GetCorrelationId(),
                skipInstruct: skipInstruct,
                calculationEditMode: CalculationEditMode.User);
        }

        [Route("api/calcs/calculation-version-history")]
        [HttpGet]
        [Produces(typeof(IEnumerable<CalculationVersionResponseModel>))]
        public Task<IActionResult> CalculationVersionHistory([FromQuery] string calculationId) => _calcsService.GetCalculationHistory(calculationId);

        [Route("api/calcs/calculation-versions")]
        [HttpPost]
        [Produces(typeof(IEnumerable<CalculationVersionResponseModel>))]
        public Task<IActionResult> CalculationVersions([FromBody] CalculationVersionsCompareModel calculationVersionsCompareModel) =>
            _calcsService.GetCalculationVersions(calculationVersionsCompareModel);

        [Route("api/calcs/calculation-edit-status")]
        [HttpPut]
        [Produces(typeof(PublishStatusResultModel))]
        public Task<IActionResult> CalculationEditStatus([FromQuery] string calculationId,
            [FromBody] EditStatusModel editStatusModel) => _calcsService.UpdateCalculationStatus(calculationId, editStatusModel);

        [Route("api/calcs/compile-preview")]
        [HttpPost]
        [Produces(typeof(PreviewResponse))]
        public Task<IActionResult> CompilePreview([FromBody] PreviewRequest previewRequest) => _previewService.Compile(previewRequest);

        [Route("api/calcs/get-buildproject-by-specification-id")]
        [HttpGet]
        [Produces(typeof(BuildProject))]
        public Task<IActionResult> GetBuildProjectBySpecificationId([FromQuery] string specificationId) => _buildProjectsService.GetBuildProjectBySpecificationId(specificationId);

        /// <summary>
        /// Get calculation code context for intellisense
        /// </summary>
        /// <param name="specificationId">Specifcation Id</param>
        /// <returns></returns>
        [Route("api/calcs/get-calculation-code-context")]
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<TypeInformation>))]
        [ProducesResponseType(419, Type = typeof(string))]
        public Task<ActionResult<IEnumerable<TypeInformation>>> GetCalculationCodeContext([FromQuery] string specificationId) => _calcsService.GetCalculationCodeContext(specificationId);

        [Route("api/calcs/update-buildproject-relationships")]
        [HttpPost]
        [Produces(typeof(BuildProject))]
        public Task<IActionResult> UpdateBuildProjectRelationships([FromQuery] string specificationId,
            [FromBody] DatasetRelationshipSummary relationship) => _buildProjectsService.UpdateBuildProjectRelationships(specificationId, relationship);

        [Route("api/calcs/reindex")]
        [HttpGet]
        [ProducesResponseType(201)]
        public Task<IActionResult> CalculationReIndex() => _calcsService.ReIndex();

        [Route("api/calcs/status-counts")]
        [HttpPost]
        [Produces(typeof(IEnumerable<CalculationStatusCountsModel>))]
        public Task<IActionResult> GetCalculationStatusCounts([FromBody] SpecificationListModel specifications) => _calcsService.GetCalculationStatusCounts(specifications);

        [Route("api/calcs/{specificationId}/assembly")]
        [HttpGet]
        [Produces(typeof(byte[]))]
        public Task<IActionResult> GetAssemblyBySpecificationId([FromRoute] string specificationId) => _buildProjectsService.GetAssemblyBySpecificationId(specificationId);

        [Route("api/calcs/validate-calc-name/{specificationId}/{calculationName}/{existingCalculationId?}")]
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> ValidationCalculationName([FromRoute] string specificationId,
            [FromRoute] string calculationName,
            [FromRoute] string existingCalculationId) => await _calcsService.IsCalculationNameValid(specificationId, calculationName, existingCalculationId);

        [Route("api/calcs/{specificationId}/compileAndSaveAssembly")]
        [HttpGet]
        [ProducesResponseType(201)]
        public async Task<IActionResult> CompileAndSaveAssembly([FromRoute] string specificationId) => await _buildProjectsService.CompileAndSaveAssembly(specificationId);

        [Route("api/calcs/{specificationId}/sourceFiles/release")]
        [HttpPost]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SaveSourceFilesRelease([FromRoute] string specificationId) =>
            await _buildProjectsService.GenerateAndSaveSourceProject(specificationId, SourceCodeType.Release);

        [Route("api/calcs/{specificationId}/sourceFiles/preview")]
        [HttpPost]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SaveSourceFilesPreview([FromRoute] string specificationId) =>
            await _buildProjectsService.GenerateAndSaveSourceProject(specificationId, SourceCodeType.Preview);

        [Route("api/calcs/{specificationId}/sourceFiles/diagnostics")]
        [HttpPost]
        [ProducesResponseType(201)]
        public async Task<IActionResult> SaveSourceFilesDiagnostics([FromRoute] string specificationId) =>
            await _buildProjectsService.GenerateAndSaveSourceProject(specificationId, SourceCodeType.Diagnostics);

        [Route("api/calcs/calculation-by-name")]
        [HttpPost]
        [Produces(typeof(CalculationResponseModel))]
        public async Task<IActionResult> GetCalculationByName([FromBody] CalculationGetModel model) => await _calcsService.GetCalculationByName(model);

        [Route("api/calcs/specifications/{specificationId}/calculations/metadata")]
        [HttpGet]
        [Produces(typeof(IEnumerable<CalculationMetadata>))]
        public async Task<IActionResult> GetCalculationsMetadata([FromRoute] string specificationId) => await _calcsService.GetCalculationsMetadataForSpecification(specificationId);

        [Route("api/calcs/specifications/{specificationId}/calculations")]
        [HttpPost]
        [Produces(typeof(CalculationResponseModel))]
        public async Task<IActionResult> CreateAdditionalCalculation([FromRoute] string specificationId,
            [FromBody] CalculationCreateModel model) =>
            await _calcsService.CreateAdditionalCalculation(specificationId, model, ControllerContext.HttpContext.Request.GetUserOrDefault(), ControllerContext.HttpContext.Request.GetCorrelationId());

        [Route("api/calcs/specifications/{specificationId}/calculations/{skipCalcRun}/{skipQueueCodeContextCacheUpdate}/{overrideCreateModelAuthor}/{updateBuildProject?}")]
        [HttpPost]
        [Produces(typeof(CalculationResponseModel))]
        public async Task<IActionResult> CreateAdditionalCalculation(
            [FromRoute] string specificationId,
            [FromBody] CalculationCreateModel model,
            [FromRoute] bool skipCalcRun,
            [FromRoute] bool skipQueueCodeContextCacheUpdate,
            [FromRoute] bool overrideCreateModelAuthor,
            [FromRoute] bool updateBuildProject = false)
            => await _calcsService.CreateAdditionalCalculation(
                specificationId, 
                model, 
                ControllerContext.HttpContext.Request.GetUserOrDefault(), 
                ControllerContext.HttpContext.Request.GetCorrelationId(),
                skipCalcRun: skipCalcRun,
                skipQueueCodeContextCacheUpdate: skipQueueCodeContextCacheUpdate,
                overrideCreateModelAuthor: overrideCreateModelAuthor,
                updateBuildProject: updateBuildProject);

        [Route("api/calcs/specifications/{specificationId}/calculations/queue-calculation-run")]
        [HttpPost]
        [Produces(typeof(Job))]
        public async Task<IActionResult> QueueCalculationRun([FromRoute] string specificationId,
            [FromBody] QueueCalculationRunModel model)
            => await _calcsService.QueueCalculationRun(specificationId, model);


        [Route("api/calcs/specifications/{specificationId}/templates/{fundingStreamId}")]
        [HttpPut]
        [Produces(typeof(TemplateMappingSummary))]
        public async Task<IActionResult> ProcessTemplateMappings([FromRoute] string specificationId,
            [FromRoute] string fundingStreamId,
            [FromBody] string templateId) => await _calcsService.ProcessTemplateMappings(specificationId, templateId, fundingStreamId);

        [Route("api/calcs/specifications/{specificationId}/templatemapping/{fundingStreamId}")]
        [HttpGet]
        [Produces(typeof(TemplateMappingSummary))]
        public async Task<IActionResult> GetMappedCalculationsOfSpecificationTemplate([FromRoute] string specificationId,
            [FromRoute] string fundingStreamId) => await _calcsService.GetMappedCalculationsOfSpecificationTemplate(specificationId, fundingStreamId);

        [Route("api/calcs/specifications/{specificationId}/templateCalculations/allApproved")]
        [HttpGet]
        [Produces(typeof(BooleanResponseModel))]
        public async Task<IActionResult> CheckHasAllApprovedTemplateCalculationsForSpecificationId([FromRoute] string specificationId) =>
            await _calcsService.CheckHasAllApprovedTemplateCalculationsForSpecificationId(specificationId);

        [HttpPost("api/calcs/specifications/{specificationId}/relationships/reindex")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> QueueReIndexSpecificationCalculationsRelationships([FromRoute] string specificationId) =>
            await _calculationRelationships.QueueForSpecification(specificationId);

        [HttpGet("api/calcs/{calculationId}/root-funding-lines")]
        [Produces(typeof(IEnumerable<CalculationFundingLine>))]
        public async Task<IActionResult> GetRootFundingLinesForCalculation([FromRoute] string calculationId)
            => await _calculationFundingLineQueryService.GetCalculationFundingLines(calculationId);

        [HttpPost("api/calcs/specifications/{specificationId}/code-context/update")]
        [Produces(typeof(Job))]
        public async Task<IActionResult> QueueUpdateCodeContext([FromRoute] string specificationId)
            => await _codeContextCache.QueueCodeContextCacheUpdate(specificationId);

        [HttpPost("api/calcs/specifications/{specificationId}/templatecalculations-update/{datasetRelationshipId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateTemplateCalculations([FromRoute] string specificationId, string datasetRelationshipId)
        {
            return await _calcsService.UpdateTemplateCalculationsForSpecification(specificationId, datasetRelationshipId, Request.GetUserOrDefault());
        }

        [HttpPost("api/calcs/specifications/{specificationId}/approve-all-calculations")]
        [Produces(typeof(Job))]
        public async Task<IActionResult> QueueApproveAllSpecificationCalculations([FromRoute] string specificationId)
        {
            HttpRequest httpRequest = ControllerContext.HttpContext.Request;

            return await _calcsService.QueueApproveAllSpecificationCalculations(specificationId, httpRequest.GetUser(), httpRequest.GetCorrelationId());
        }

        [HttpPost("api/calcs/{specificationId}/{datasetDefinitionRelationshipId}/remap")]
        [Produces(typeof(JobViewModel))]
        public async Task<IActionResult> ReMapSpecificationRelationship([FromRoute] string specificationId, [FromRoute] string datasetDefinitionRelationshipId)
        {
            Reference user = Request.GetUser();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return await _referencedSpecificationReMapService.QueueReferencedSpecificationReMapJob(specificationId,
                datasetDefinitionRelationshipId,
                user,
                correlationId);
        }

        [Route("api/calcs/generate-identifier")]
        [HttpPost]
        [Produces(typeof(CalculationIdentifier))]
        public IActionResult GenerateCalculationIdentifier([FromBody] GenerateIdentifierModel generateIdentifierModel) 
            => _calcsService.GenerateCalculationIdentifier(generateIdentifierModel);

        [Route("api/calcs/remove-calculation-for-specification/{specificationId}/{calcType}")]
        [HttpDelete]
        [ProducesResponseType(200)]
        public async Task RemoveCalculationForSpecification([FromRoute] string specificationId, [FromRoute] string calcType) => await _calcsSearchService.RemoveCalculations(specificationId, calcType);
    }
}