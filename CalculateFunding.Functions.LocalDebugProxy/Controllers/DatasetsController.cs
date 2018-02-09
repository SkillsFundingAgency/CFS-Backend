using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class DatasetsController : BaseController
    {
        private readonly IDefinitionsService _definitionService;
        private readonly IDatasetService _datasetService;
        private readonly IDatasetSearchService _datasetSearchService;

        public DatasetsController(IServiceProvider serviceProvider, 
            IDefinitionsService definitionService, IDatasetService datasetService, IDatasetSearchService datasetSearchService) 
            : base (serviceProvider)
        {
            Guard.ArgumentNotNull(definitionService, nameof(definitionService));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));
            Guard.ArgumentNotNull(datasetSearchService, nameof(datasetSearchService));

            _definitionService = definitionService;
            _datasetService = datasetService;
            _datasetSearchService = datasetSearchService;
        }

        [Route("api/datasets/data-definitions")]
        [HttpPost]
        public Task<IActionResult> RunDataDefinitionSave()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionService.SaveDefinition(ControllerContext.HttpContext.Request);
        }

        [Route("api/datasets/data-definitions")]
        [HttpGet]
        public Task<IActionResult> RunGetDataDefinitions()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionService.GetDatasetDefinitions(ControllerContext.HttpContext.Request);
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
    }
}
