using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Datasets.Controllers
{
    public class DatasetsController : ControllerBase
    {
        private readonly IDefinitionsService _definitionService;
        private readonly IDatasetService _datasetService;
        private readonly IDatasetSearchService _datasetSearchService;
        private readonly IDatasetDefinitionSearchService _datasetDefinitionSearchService;
        private readonly IDefinitionSpecificationRelationshipService _definitionSpecificationRelationshipService;
        private readonly IProcessDatasetService _processDatasetService;

        public DatasetsController(
            IDefinitionsService definitionService,
            IDatasetService datasetService,
            IDatasetSearchService datasetSearchService,
            IDatasetDefinitionSearchService datasetDefinitionSearchService,
            IDefinitionSpecificationRelationshipService definitionSpecificationRelationshipService,
            IProcessDatasetService processDatasetService)
        {
            Guard.ArgumentNotNull(definitionService, nameof(definitionService));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));
            Guard.ArgumentNotNull(datasetSearchService, nameof(datasetSearchService));
            Guard.ArgumentNotNull(datasetDefinitionSearchService, nameof(datasetDefinitionSearchService));
            Guard.ArgumentNotNull(definitionSpecificationRelationshipService, nameof(definitionSpecificationRelationshipService));
            Guard.ArgumentNotNull(processDatasetService, nameof(processDatasetService));

            _definitionService = definitionService;
            _datasetService = datasetService;
            _datasetSearchService = datasetSearchService;
            _datasetDefinitionSearchService = datasetDefinitionSearchService;
            _definitionSpecificationRelationshipService = definitionSpecificationRelationshipService;
            _processDatasetService = processDatasetService;
        }

        /// <remarks>
        /// 
        /// </remarks>
        [Route("api/datasets/data-definitions")]
        [HttpPost]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DataDefinitionSave()
        {
            string yaml = await ControllerContext.HttpContext.Request.GetRawBodyStringAsync();
            string yamlFilename = ControllerContext.HttpContext.Request.GetYamlFileNameFromRequest();

            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();
            
            return await _definitionService.SaveDefinition(yaml, yamlFilename, user, correlationId);
        }

        [Route("api/datasets/get-data-definitions")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetDefinition>))]
        public Task<IActionResult> GetDatasetDefinitions()
        {
            return _definitionService.GetDatasetDefinitions();
        }

        [Route("api/datasets/get-dataset-definition-by-id")]
        [HttpGet]
        [Produces(typeof(DatasetDefinition))]
        public Task<IActionResult> GetDatasetDefinitionById([FromQuery] string datasetDefinitionId)
        {
            return _definitionService.GetDatasetDefinitionById(datasetDefinitionId);
        }

        [Route("api/datasets/get-dataset-definitions-by-ids")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetDefinition>))]
        public Task<IActionResult> GetDatasetDefinitionsById([FromBody] IEnumerable<string> definitionIds)
        {
            return _definitionService.GetDatasetDefinitionsByIds(definitionIds);
        }

        [Route("api/datasets/create-new-dataset")]
        [HttpPost]
        [Produces(typeof(NewDatasetVersionResponseModel))]
        public Task<IActionResult> CreateDataset([FromBody] CreateNewDatasetModel createNewDatasetModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            return _datasetService.CreateNewDataset(createNewDatasetModel, user);
        }

        [Route("api/datasets/dataset-version-update")]
        [HttpPost]
        [Produces(typeof(NewDatasetVersionResponseModel))]
        public Task<IActionResult> DatasetVersionUpdate([FromBody] DatasetVersionUpdateModel datasetVersionUpdateModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            return _datasetService.DatasetVersionUpdate(datasetVersionUpdateModel, user);
        }

        [Route("api/datasets/datasets-search")]
        [HttpPost]
        [Produces(typeof(DatasetSearchResults))]
        public Task<IActionResult> DatasetsSearch([FromBody] SearchModel searchModel)
        {
            return _datasetSearchService.SearchDatasets(searchModel);
        }

        [Route("api/datasets/datasets-version-search")]
        [HttpPost]
        [Produces(typeof(DatasetVersionSearchResults))]
        public Task<IActionResult> DatasetsVersionSearch([FromBody] SearchModel searchModel)
        {
            return _datasetSearchService.SearchDatasetVersion(searchModel);
        }

        [Route("api/datasets/dataset-definitions-search")]
        [HttpPost]
        [Produces(typeof(DatasetDefinitionSearchResults))]
        public Task<IActionResult> DatasetDefinitionsSearch([FromBody] SearchModel searchModel)
        {
            return _datasetDefinitionSearchService.SearchDatasetDefinitions(searchModel);
        }

        [Route("api/datasets/validate-dataset")]
        [HttpPost]
        [Produces(typeof(DatasetValidationStatusModel))]
        public Task<IActionResult> ValidateDataset([FromBody] GetDatasetBlobModel getDatasetBlobModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return _datasetService.ValidateDataset(getDatasetBlobModel, user, correlationId);
        }

        [Route("api/datasets/create-definitionspecification-relationship")]
        [HttpPost]
        [Produces(typeof(DefinitionSpecificationRelationship))]
        public Task<IActionResult> CreateDefinitionSpecificationRelationship([FromBody] CreateDefinitionSpecificationRelationshipModel createDefinitionSpecificationRelationshipModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return _definitionSpecificationRelationshipService.CreateRelationship(createDefinitionSpecificationRelationshipModel, user, correlationId);
        }

        [Route("api/specifications/{specificationId}/datasets/edit-definition-specification-relationship/{relationshipId}")]
        [HttpPut]
        [Produces(typeof(DefinitionSpecificationRelationshipVersion))]
        public Task<IActionResult> UpdateDefinitionSpecificationRelationship([FromRoute] string specificationId, [FromRoute] string relationshipId,
            [FromBody] UpdateDefinitionSpecificationRelationshipModel editDefinitionSpecificationRelationshipModel)
        {
            return _definitionSpecificationRelationshipService.UpdateRelationship(editDefinitionSpecificationRelationshipModel, specificationId, relationshipId);
        }

        [Route("api/datasets/validate-definitionspecification-relationship")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public Task<IActionResult> ValidateDefinitionSpecificationRelationship([FromBody] ValidateDefinitionSpecificationRelationshipModel validateDefinitionSpecificationRelationshipModel)
        {
            return _definitionSpecificationRelationshipService.ValidateRelationship(validateDefinitionSpecificationRelationshipModel);
        }

        [Route("api/datasets/migrate-definitionspecification-relationships")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public Task<IActionResult> MigrateDefinitionSpecificationRelationship()
        {
            return _definitionSpecificationRelationshipService.Migrate();
        }

        [Route("api/datasets/get-definitions-relationships")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetSpecificationRelationshipViewModel>))]
        public Task<IActionResult> GetDefinitionRelationships([FromQuery] string specificationId)
        {
            return _definitionSpecificationRelationshipService.GetRelationshipsBySpecificationIdResult(specificationId);
        }

        [Route("api/datasets/definition-relationships/{relationshipId}/get-funding-line-calculations")]
        [HttpGet]
        [Produces(typeof(PublishedSpecificationConfiguration))]
        public Task<IActionResult> GetFundingLinesCalculations([FromRoute] string relationshipId)
        {
            return _definitionSpecificationRelationshipService.GetFundingLineCalculations(relationshipId);
        }

        [Route("api/datasets/get-definition-relationship-by-specificationid-name")]
        [HttpGet]
        [Produces(typeof(DefinitionSpecificationRelationship))]
        public Task<IActionResult> GetDefinitionRelationshipBySpecificationIdAndName([FromQuery] string specificationId, [FromQuery] string name)
        {
            return _definitionSpecificationRelationshipService.GetRelationshipBySpecificationIdAndName(specificationId, name);
        }

        [Route("api/datasets/get-datasets-by-definitionid")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetViewModel>))]
        public Task<IActionResult> GetDatasetsByDefinitionId([FromQuery] string definitionId)
        {
            return _datasetService.GetDatasetsByDefinitionId(definitionId);
        }

        [Route("api/datasets/get-relationships-by-specificationId")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetSpecificationRelationshipViewModel>))]
        public Task<IActionResult> GetRelationshipsBySpecificationId([FromQuery] string specificationId)
        {
            return _definitionSpecificationRelationshipService.GetCurrentRelationshipsBySpecificationId(specificationId);
        }

        [Route("api/datasets/get-datasources-by-relationshipid")]
        [HttpGet]
        [Produces(typeof(SelectDatasourceModel))]
        public Task<IActionResult> GetDataSourcesByRelationshipId([FromQuery] string relationshipId)
        {
            return _definitionSpecificationRelationshipService.GetDataSourcesByRelationshipId(relationshipId);
        }

        [Route("api/datasets/assign-datasource-to-relationship")]
        [HttpPost]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public Task<IActionResult> AssignDatasourceVersionToRelationship([FromBody] AssignDatasourceModel assignDatasourceModel)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return _definitionSpecificationRelationshipService.AssignDatasourceVersionToRelationship(assignDatasourceModel, user, correlationId);
        }

        [Route("api/datasets/download-dataset-file")]
        [HttpGet]
        [Produces(typeof(DatasetDownloadModel))]
        public Task<IActionResult> DownloadDatasetFile([FromQuery] string datasetId, [FromQuery] string datasetVersion)
        {
            return _datasetService.DownloadDatasetFile(datasetId, datasetVersion);
        }

        [Route("api/datasets/download-dataset-merge-file")]
        [HttpGet]
        [Produces(typeof(DatasetDownloadModel))]
        public Task<IActionResult> DownloadDatasetMergeFile([FromQuery] string datasetId, [FromQuery] string datasetVersion)
        {
            return _datasetService.DownloadOriginalDatasetUploadFile(datasetId, datasetVersion);
        }

        [HttpPost("api/datasets/upload-dataset-file/{filename}")]
        [DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UploadDatasetFile([FromRoute]string filename, [FromBody]DatasetMetadataViewModel datasetMetadataViewModel)
        {
            return await _datasetService.UploadDatasetFile(filename, datasetMetadataViewModel);
        }

        [Route("api/datasets/reindex")]
        [HttpGet]
        [Produces(typeof(string))]
        public Task<IActionResult> ReindexDatasets()
        {
            return _datasetService.Reindex();
        }

        [Route("api/datasetsversions/reindex")]
        [HttpGet]
        [Produces(typeof(string))]
        public Task<IActionResult> ReindexDatasetsVersions()
        {
            return _datasetService.ReindexDatasetVersions();
        }

        [Route("api/datasets/get-currentdatasetversion-by-datasetid")]
        [HttpGet]
        [Produces(typeof(DatasetVersionResponseViewModel))]
        public Task<IActionResult> GetCurrentDatasetVersionByDatasetId([FromQuery] string datasetId)
        {
            return _datasetService.GetCurrentDatasetVersionByDatasetId(datasetId);
        }

        [Route("api/datasets/get-dataset-by-datasetid")]
        [HttpGet]
        [Produces(typeof(DatasetViewModel))]
        public Task<IActionResult> GetDatasetByDatasetId([FromQuery] string datasetId)
        {
            return _datasetService.GetDatasetByDatasetId(datasetId);
        }

        [Route("api/datasets/get-schema-download-url")]
        [HttpPost]
        [Produces(typeof(DatasetSchemaSasUrlResponseModel))]
        public Task<IActionResult> GetDatasetSchemaSasUrl([FromBody] DatasetSchemaSasUrlRequestModel datasetSchemaSasUrlRequestModel)
        {
            return _definitionService.GetDatasetSchemaSasUrl(datasetSchemaSasUrlRequestModel);
        }

        [Route("api/datasets/regenerate-providersourcedatasets")]
        [HttpPost]
        [Produces(typeof(IEnumerable<DefinitionSpecificationRelationship>))]
        public Task<IActionResult> RegenerateProviderSourceDatasets([FromQuery] string specificationId)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return _datasetService.RegenerateProviderSourceDatasets(specificationId, user, correlationId);
        }

        [Route("api/datasets/get-dataset-validate-status")]
        [HttpGet]
        [Produces(typeof(DatasetValidationStatusModel))]
        public Task<IActionResult> GetValidateDatasetStatus([FromQuery] string operationId)
        {
            return _datasetService.GetValidateDatasetStatus(operationId);
        }

        [Route("api/datasets/{specificationId}/datasetAggregations")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetAggregations>))]
        public async Task<IActionResult> GetDatasetAggregations(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                return new BadRequestObjectResult("Misssing specification id");
            }

            return await _processDatasetService.GetDatasetAggregationsBySpecificationId(specificationId);
        }

        [Route("api/datasets/{specificationId}/schemaRelationshipFields")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetSchemaRelationshipModel>))]
        public async Task<IActionResult> GetSchemaRelationshipsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                return new BadRequestObjectResult("Misssing specification id");
            }

            return await _definitionSpecificationRelationshipService.GetCurrentDatasetRelationshipFieldsBySpecificationId(specificationId);
        }

        [Route("api/datasets/{datasetDefinitionId}/relationshipSpecificationIds")]
        [HttpGet]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetSpecificationIdsForRelationshipDefinitionId(string datasetDefinitionId)
        {
            if (string.IsNullOrWhiteSpace(datasetDefinitionId))
            {
                return new BadRequestObjectResult($"Missing {nameof(datasetDefinitionId)}");
            }

            return await _definitionSpecificationRelationshipService.GetSpecificationIdsForRelationshipDefinitionId(datasetDefinitionId);
        }

        [Route("api/datasets/{specificationId}/{datasetDefinitionId}/relationships")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetSpecificationRelationshipViewModel>))]
        public async Task<IActionResult> GetRelationshipsBySpecificationIdDasetDefinitionId(string specificationId, string datasetDefinitionId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                return new BadRequestObjectResult("Missing specification id");
            }

            if (string.IsNullOrWhiteSpace(datasetDefinitionId))
            {
                return new BadRequestObjectResult("Missing dataset definition id");
            }

            return await _definitionSpecificationRelationshipService.GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(specificationId, datasetDefinitionId);
        }

        [Route("api/datasets/get-data-definitions/{fundingStreamId}")]
        [HttpGet]
        [Produces(typeof(IEnumerable<DatasetDefinition>))]
        public Task<IActionResult> GetDatasetDefinitionsByFundingStreamId(string fundingStreamId)
        {
            return _definitionService.GetDatasetDefinitionsByFundingStreamId(fundingStreamId);
        }

        [Route("api/datasets/data-definitions/{datasetDefinitionId}")]
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateDatasetDefinition([FromRoute]int datasetDefinitionId, [FromBody] CreateDatasetDefinitionFromTemplateModel createDatasetDefinitionModel)
        {
            if(datasetDefinitionId == 0)
            {
                return new BadRequestObjectResult("DatasetDefinition Id must be provided");
            }

            if (createDatasetDefinitionModel == null)
            {
                return new BadRequestObjectResult("Missing CreateDatasetDefinitionModel details");
            }

            createDatasetDefinitionModel.DatasetDefinitionId = datasetDefinitionId;
            
            return await _definitionService.CreateOrUpdateDatasetDefinition(createDatasetDefinitionModel, Request.GetCorrelationId(), Request.GetUserOrDefault());
        }

        [Route("api/datasets/get-validate-dataset-error-url")]
        [HttpPost]
        [Produces(typeof(DatasetValidationErrorSasUrlResponseModel))]
        public IActionResult GetValidateDatasetValidationErrorUrl([FromBody] DatasetValidationErrorRequestModel requestModel)
        {
            if (string.IsNullOrWhiteSpace(requestModel?.JobId))
            {
                return new BadRequestObjectResult($"Missing {nameof(requestModel.JobId)} details");
            }

            return _datasetService.GetValidateDatasetValidationErrorSasUrl(requestModel);
        }

        [Route("api/datasets/toggleDatasetSchema/{relationshipId}")]
        [HttpPut]
        [Produces(typeof(HttpStatusCode))]
        public Task<IActionResult> ToggleDatasetRelationship([FromRoute] string relationshipId, [FromBody] bool converterEnabled)
        {
            return _definitionSpecificationRelationshipService.ToggleDatasetRelationship(relationshipId, converterEnabled);
        }

        [Route("api/datasets/fixup-datasets-fundingstream")]
        [HttpGet]
        [Produces(typeof(string))]
        public Task<IActionResult> FixupDatasetsFundingStream()
        {
            return _datasetService.FixupDatasetsFundingStream();
        }
    }
}
