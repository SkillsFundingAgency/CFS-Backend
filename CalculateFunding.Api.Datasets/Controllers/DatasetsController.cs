using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Datasets.Controllers
{
    public class DatasetsController : Controller
    {
        private readonly IDefinitionsService _definitionService;
        private readonly IDatasetService _datasetService;
        private readonly IDatasetSearchService _datasetSearchService;
        private readonly IDefinitionSpecificationRelationshipService _definitionSpecificationRelationshipService;

        public DatasetsController(
            IDefinitionsService definitionService,
            IDatasetService datasetService,
            IDatasetSearchService datasetSearchService,
            IDefinitionSpecificationRelationshipService definitionSpecificationRelationshipService)
        {
            Guard.ArgumentNotNull(definitionService, nameof(definitionService));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));
            Guard.ArgumentNotNull(datasetSearchService, nameof(datasetSearchService));
            Guard.ArgumentNotNull(definitionSpecificationRelationshipService, nameof(definitionSpecificationRelationshipService));

            _definitionService = definitionService;
            _datasetService = datasetService;
            _datasetSearchService = datasetSearchService;
            _definitionSpecificationRelationshipService = definitionSpecificationRelationshipService;
        }

        [Route("api/datasets/data-definitions")]
        [HttpPost]
        public Task<IActionResult> RunDataDefinitionSave()
        {
            return _definitionService.SaveDefinition(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-data-definitions")]
        [HttpGet]
        public Task<IActionResult> RunGetDatasetDefinitions()
        {
            return _definitionService.GetDatasetDefinitions(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-dataset-definition-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetDatasetDefinitionById()
        {
            return _definitionService.GetDatasetDefinitionById(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-dataset-definitions-by-ids")]
        [HttpGet]
        public Task<IActionResult> RunGetDatasetDefinitionsById()
        {
            return _definitionService.GetDatasetDefinitionsByIds(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/create-new-dataset")]
        [HttpPost]
        public Task<IActionResult> RunCreateDataset()
        {
            return _datasetService.CreateNewDataset(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/dataset-version-update")]
        [HttpPost]
        public Task<IActionResult> RunDatasetVersionUpdate()
        {
            return _datasetService.DatasetVersionUpdate(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/datasets-search")]
        [HttpPost]
        public Task<IActionResult> RunDatasetsSearch()
        {
            return _datasetSearchService.SearchDatasets(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/validate-dataset")]
        [HttpPost]
        public Task<IActionResult> RunValidateDataset()
        {
            return _datasetService.ValidateDataset(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/create-definitionspecification-relationship")]
        [HttpPost]
        public Task<IActionResult> RunCreateDefinitionSpecificationRelationship()
        {
            return _definitionSpecificationRelationshipService.CreateRelationship(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-definitions-relationships")]
        [HttpGet]
        public Task<IActionResult> RunGetDefinitionRelationships()
        {
            return _definitionSpecificationRelationshipService.GetRelationshipsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-definition-relationship-by-specificationid-name")]
        [HttpGet]
        public Task<IActionResult> RunGetDefinitionRelationshipBySpecificationIdAndName()
        {
            return _definitionSpecificationRelationshipService.GetRelationshipBySpecificationIdAndName(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-datasets-by-definitionid")]
        [HttpGet]
        public Task<IActionResult> RunGetDatasetsByDefinitionId()
        {
            return _datasetService.GetDatasetsByDefinitionId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-relationships-by-specificationId")]
        [HttpGet]
        public Task<IActionResult> RunGetRealtionshipsBySpecificationId()
        {
            return _definitionSpecificationRelationshipService.GetCurrentRelationshipsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-datasources-by-relationshipid")]
        [HttpGet]
        public Task<IActionResult> RunGetDataSourcesByRelationshipId()
        {
            return _definitionSpecificationRelationshipService.GetDataSourcesByRelationshipId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/assign-datasource-to-relationship")]
        [HttpPost]
        public Task<IActionResult> RunAssignDatasourceVersionToRelationship()
        {
            return _definitionSpecificationRelationshipService.AssignDatasourceVersionToRelationship(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/download-dataset-file")]
        [HttpGet]
        public Task<IActionResult> RunDownloadDatasetFile()
        {
            return _datasetService.DownloadDatasetFile(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/reindex")]
        [HttpGet]
        public Task<IActionResult> RunReindexDatasets()
        {
            return _datasetService.Reindex(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-currentdatasetversion-by-datasetid")]
        [HttpGet]
        public Task<IActionResult> RunGetCurrentDatasetVersionByDatasetId()
        {
            return _datasetService.GetCurrentDatasetVersionByDatasetId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/process-dataset")]
        [HttpPost]
        public Task<IActionResult> RunProcessDataset()
        {
            return _datasetService.ProcessDataset(ControllerContext.HttpContext.Request);
        }
    }
}
