using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using Newtonsoft.Json.Linq;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class EventHubsController : BaseController
    {
        private readonly IBuildProjectsService _buildProjectService;
        private readonly IDatasetService _datsetService;
        private readonly IResultsService _resultsService;
        private readonly ICalculationEngineService _calculationEngineService;
        private readonly ICalculationService _calculationService;
        private readonly ISpecificationsService _specificationsService;

        public EventHubsController(
            IServiceProvider serviceProvider, 
            IBuildProjectsService buildProjectService, 
            IDatasetService datsetService, 
            IResultsService resultsService,
            ICalculationEngineService calculationEngineService,
            ICalculationService calculationService,
            ISpecificationsService specificationsService) : base(serviceProvider)
        {
            _buildProjectService = buildProjectService;
            _datsetService = datsetService;
            _resultsService = resultsService;
            _calculationEngineService = calculationEngineService;
            _calculationService = calculationService;
            _specificationsService = specificationsService;
        }

        [Route("api/events/calc-events-generate-allocations-results")]
        [HttpPost]
        async public Task RunGenerateAllocationResults()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            EventData message = await GeEventMessage(ControllerContext.HttpContext.Request);

            await _calculationEngineService.GenerateAllocations(message);
        }

        [Route("api/events/calc-events-instruct-generate-allocations")]
        [HttpPost]
        async public Task RunUpdateAllocations()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            EventData message = await GeEventMessage(ControllerContext.HttpContext.Request);

            await _buildProjectService.UpdateAllocations(message);
        }

        [Route("api/events/calc-events-add-relationship-to-buildproject")]
        [HttpPost]
        async public Task RunAddRelationshipToBuildProject()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            EventData message = await GeEventMessage(ControllerContext.HttpContext.Request);

            await _buildProjectService.UpdateBuildProjectRelationships(message);
        }

        [Route("api/events/calc-events-create-draft")]
        [HttpPost]
        async public Task RunCreateDraftCalc()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            EventData message = await GeEventMessage(ControllerContext.HttpContext.Request);

            await _calculationService.CreateCalculation(message);
        }

        [Route("api/events/dataset-events-datasets")]
        [HttpPost]
        async public Task RunProcessDatasets()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            EventData message = await GeEventMessage(ControllerContext.HttpContext.Request);

            await _datsetService.ProcessDataset(message);
        }

        [Route("api/events/dataset-events-results")]
        [HttpPost]
        async public Task RunUpdateDatasetResults()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            EventData message = await GeEventMessage(ControllerContext.HttpContext.Request);

            await _resultsService.UpdateProviderData(message);
        }

        [Route("api/events/spec-events-add-definition-relationship")]
        [HttpPost]
        async public Task RunAddDefinitionSpecificationService()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            EventData message = await GeEventMessage(ControllerContext.HttpContext.Request);

            await _specificationsService.AssignDataDefinitionRelationship(message);
        }

        async Task<EventData> GeEventMessage(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            dynamic payload = JsonConvert.DeserializeObject<dynamic>(json);

            JArray bytes = payload["body"] as JArray;

            byte[] body = bytes.Select(z => byte.Parse(z.ToString())).ToArray();

            JObject propertiesDictionary = payload["properties"] as JObject;

            Dictionary<string, string> properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(propertiesDictionary.ToString());

            EventData message = new EventData(body);
            foreach(var property in properties)
            {
                message.Properties.Add(property.Key, property.Value);
            }

            return message;
        }
    }
}
