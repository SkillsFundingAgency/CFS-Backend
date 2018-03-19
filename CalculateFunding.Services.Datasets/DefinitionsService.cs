using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionsService : IDefinitionsService
    {
        private readonly ILogger _logger;
        private readonly IDatasetRepository _dataSetsRepository;

        public DefinitionsService(ILogger logger, IDatasetRepository dataSetsRepository)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(dataSetsRepository, nameof(dataSetsRepository));

            _logger = logger;
            _dataSetsRepository = dataSetsRepository;
        }

        async public Task<IActionResult> SaveDefinition(HttpRequest request)
        {
            string yaml = await request.GetRawBodyStringAsync();

            string yamlFilename = GetYamlFileNameFromRequest(request);

            if (string.IsNullOrEmpty(yaml))
            {
                _logger.Error($"Null or empty yaml provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            DatasetDefinition definition = null;

            try
            {
                definition = deserializer.Deserialize<DatasetDefinition>(yaml);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Invalid yaml was provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            try
            {
                HttpStatusCode result = await _dataSetsRepository.SaveDefinition(definition);
                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    _logger.Error($"Failed to save yaml file: {yamlFilename} to cosmos db with status {statusCode}");

                    return new StatusCodeResult(statusCode);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Exception occurred writing to yaml file: {yamlFilename} to cosmos db");

                return new StatusCodeResult(500);
            }

            _logger.Information($"Successfully saved file: {yamlFilename} to cosmos db");

            return new OkResult();
        }

        async public Task<IActionResult> GetDatasetDefinitions(HttpRequest request)
        {
            IEnumerable<DatasetDefinition> definitions = await _dataSetsRepository.GetDatasetDefinitions();

            return new OkObjectResult(definitions);
        }

        public async Task<IActionResult> GetDatasetDefinitionById(HttpRequest request)
        {
            request.Query.TryGetValue("datasetDefinitionId", out var requestDatasetDefinitionId);

            var datasetDefinitionId = requestDatasetDefinitionId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(datasetDefinitionId))
            {
                _logger.Error("No datasetDefinitionId was provided to GetDatasetDefinitionById");

                return new BadRequestObjectResult("Null or empty datasetDefinitionId provided");
            }

            DatasetDefinition defintion = await _dataSetsRepository.GetDatasetDefinition(datasetDefinitionId);
            if(defintion == null)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(defintion);
        }

        public async Task<IActionResult> GetDatasetDefinitionsByIds(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            IEnumerable<string> definitionIds = JsonConvert.DeserializeObject<IEnumerable<string>>(json);
            if (!definitionIds.Any())
            {
                _logger.Error($"No Dataset Definition Ids were provided to lookup");
                return new BadRequestObjectResult($"No DatasetDefinitionIds were provided to lookup");
            }

            IEnumerable<DatasetDefinition> defintions =  await _dataSetsRepository.GetDatasetDefinitionsByQuery(d => definitionIds.Contains(d.Id));
            return new OkObjectResult(definitionIds);
        }

        string GetYamlFileNameFromRequest(HttpRequest request)
        {
            if (request.Headers.ContainsKey("yaml-file"))
            {
                return request.Headers["yaml-file"].FirstOrDefault();
            }

            return "File name not provided";
        }


    }
}
