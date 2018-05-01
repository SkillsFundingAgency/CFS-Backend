using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class DatasetsController : BaseController
    {
        private readonly IDefinitionsService _definitionService;
        private readonly IDatasetService _datasetService;
        private readonly IDatasetSearchService _datasetSearchService;
        private readonly IDefinitionSpecificationRelationshipService _definitionSpecificationRelationshipService;

        public DatasetsController(IServiceProvider serviceProvider,
            IDefinitionsService definitionService, IDatasetService datasetService, 
            IDatasetSearchService datasetSearchService, IDefinitionSpecificationRelationshipService definitionSpecificationRelationshipService)
            : base(serviceProvider)
        {
            Guard.ArgumentNotNull(definitionService, nameof(definitionService));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));
            Guard.ArgumentNotNull(datasetSearchService, nameof(datasetSearchService));

            _definitionService = definitionService;
            _datasetService = datasetService;
            _datasetSearchService = datasetSearchService;
            _definitionSpecificationRelationshipService = definitionSpecificationRelationshipService;
        }

        [Route("api/datasets/data-definitions")]
        [HttpPost]
        public Task<IActionResult> RunDataDefinitionSave()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionService.SaveDefinition(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-data-definitions")]
        [HttpGet]
        public Task<IActionResult> RunGetDatasetDefinitions()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionService.GetDatasetDefinitions(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-dataset-definition-by-id")]
        [HttpGet]
        public Task<IActionResult> RunGetDatasetDefinitionById()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionService.GetDatasetDefinitionById(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-dataset-definitions-by-ids")]
        [HttpGet]
        public Task<IActionResult> RunGetDatasetDefinitionsById()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionService.GetDatasetDefinitionsByIds(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/create-new-dataset")]
        [HttpPost]
        public Task<IActionResult> RunCreateDataset()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _datasetService.CreateNewDataset(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/datasets-search")]
        [HttpPost]
        public Task<IActionResult> RunDatasetsSearch()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _datasetSearchService.SearchDatasets(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/validate-dataset")]
        [HttpPost]
        public Task<IActionResult> RunValidateDataset()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _datasetService.ValidateDataset(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/create-definitionspecification-relationship")]
        [HttpPost]
        public Task<IActionResult> RunCreateDefinitionSpecificationRelationship()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionSpecificationRelationshipService.CreateRelationship(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-definitions-relationships")]
        [HttpGet]
        public Task<IActionResult> RunGetDefinitionRelationships()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionSpecificationRelationshipService.GetRelationshipsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-definition-relationship-by-specificationid-name")]
        [HttpGet]
        public Task<IActionResult> RunGetDefinitionRelationshipBySpecificationIdAndName()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionSpecificationRelationshipService.GetRelationshipBySpecificationIdAndName(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-datasets-by-definitionid")]
        [HttpGet]
        public Task<IActionResult> RunGetDatasetsByDefinitionId()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _datasetService.GetDatasetsByDefinitionId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-relationships-by-specificationId")]
        [HttpGet]
        public Task<IActionResult> RunGetRealtionshipsBySpecificationId()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionSpecificationRelationshipService.GetCurrentRelationshipsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/get-datasources-by-relationshipid")]
        [HttpGet]
        public Task<IActionResult> RunGetDataSourcesByRelationshipId()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionSpecificationRelationshipService.GetDataSourcesByRelationshipId(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/assign-datasource-to-relationship")]
        [HttpPost]
        public Task<IActionResult> RunAssignDatasourceVersionToRelationship()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionSpecificationRelationshipService.AssignDatasourceVersionToRelationship(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/download-dataset-file")]
        [HttpGet]
        public Task<IActionResult> RunDownloadDatasetFile()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _datasetService.DownloadDatasetFile(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/process-dataset")]
        [HttpPost]
        public Task<IActionResult> RunProcessDataset()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _datasetService.ProcessDataset(ControllerContext.HttpContext.Request);
        }
    }
}
