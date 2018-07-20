using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionsService : IDefinitionsService
    {
        private readonly ILogger _logger;
        private readonly IDatasetRepository _datasetsRepository;
        private readonly ISearchRepository<DatasetDefinitionIndex> _datasetDefinitionSearchRepository;
        private readonly Policy _datasetDefinitionSearchRepositoryPolicy;
        private readonly Policy _datasetsRepositoryPolicy;

        public DefinitionsService(ILogger logger, IDatasetRepository dataSetsRepository, ISearchRepository<DatasetDefinitionIndex> datasetDefinitionSearchRepository, IDatasetsResiliencePolicies datasetsResiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(dataSetsRepository, nameof(dataSetsRepository));
            Guard.ArgumentNotNull(datasetDefinitionSearchRepository, nameof(datasetDefinitionSearchRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies, nameof(datasetsResiliencePolicies));

            _logger = logger;
            _datasetsRepository = dataSetsRepository;
            _datasetDefinitionSearchRepository = datasetDefinitionSearchRepository;

            _datasetDefinitionSearchRepositoryPolicy = datasetsResiliencePolicies.DatasetDefinitionSearchRepository;
            _datasetsRepositoryPolicy = datasetsResiliencePolicies.DatasetRepository;
        }

        async public Task<IActionResult> SaveDefinition(HttpRequest request)
        {
            string yaml = await request.GetRawBodyStringAsync();

            string yamlFilename = request.GetYamlFileNameFromRequest();

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
                HttpStatusCode result = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.SaveDefinition(definition));
                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    _logger.Error($"Failed to save yaml file: {yamlFilename} to cosmos db with status {statusCode}");

                    return new StatusCodeResult(statusCode);
                }

                await IndexDatasetDefinition(definition);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Exception occurred writing to yaml file: {yamlFilename} to cosmos db");

                return new InternalServerErrorResult($"Exception occurred writing to yaml file: {yamlFilename} to cosmos db");
            }

            _logger.Information($"Successfully saved file: {yamlFilename} to cosmos db");

            return new OkResult();
        }

        public async Task<IEnumerable<IndexError>> IndexDatasetDefinition(DatasetDefinition definition)
        {
            // Calculate hash for model to see if there are changes
            string modelJson = JsonConvert.SerializeObject(definition);
            string hashCode = "";

            using (SHA256 sha256Generator = SHA256Managed.Create())
            {
                byte[] modelBytes = UTF8Encoding.UTF8.GetBytes(modelJson);
                foreach (byte hashByte in sha256Generator.ComputeHash(modelBytes))
                {
                    hashCode += string.Format("{0:X2}", hashByte);
                }
            }

            DatasetDefinitionIndex datasetDefinitionIndex = new DatasetDefinitionIndex()
            {
                Id = definition.Id,
                Name = definition.Name,
                Description = definition.Description,
                LastUpdatedDate = DateTimeOffset.Now,
                ProviderIdentifier = definition.TableDefinitions.FirstOrDefault()?.FieldDefinitions?.Where(f => f.IdentifierFieldType.HasValue)?.Select(f => Enum.GetName(typeof(IdentifierFieldType), f.IdentifierFieldType.Value)).FirstOrDefault(),
                ModelHash = hashCode,
            };

            if (string.IsNullOrWhiteSpace(datasetDefinitionIndex.ProviderIdentifier))
            {
                datasetDefinitionIndex.ProviderIdentifier = "None";
            }

            bool updateIndex = true;

            // Only update index if metadata or model has changed, this is to preserve the LastUpdateDate
            DatasetDefinitionIndex existingIndex = await _datasetDefinitionSearchRepositoryPolicy.ExecuteAsync(() =>
                                                             _datasetDefinitionSearchRepository.SearchById(definition.Id));
            if (existingIndex != null)
            {
                if (existingIndex.ModelHash == hashCode &&
                        existingIndex.Description == definition.Description &&
                        existingIndex.Name == definition.Name &&
                        existingIndex.ProviderIdentifier == datasetDefinitionIndex.ProviderIdentifier)
                {
                    updateIndex = false;
                }
            }

            if (updateIndex)
            {
                return await _datasetDefinitionSearchRepositoryPolicy.ExecuteAsync(() =>
                    _datasetDefinitionSearchRepository.Index(new DatasetDefinitionIndex[] { datasetDefinitionIndex }));
            }

            return Enumerable.Empty<IndexError>();
        }

        async public Task<IActionResult> GetDatasetDefinitions(HttpRequest request)
        {
            IEnumerable<DatasetDefinition> definitions = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDatasetDefinitions());

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

            DatasetDefinition defintion = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDatasetDefinition(datasetDefinitionId));
            if (defintion == null)
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

            IEnumerable<DatasetDefinition> defintions = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDatasetDefinitionsByQuery(d => definitionIds.Contains(d.Id)));
            return new OkObjectResult(definitionIds);
        }
    }
}
