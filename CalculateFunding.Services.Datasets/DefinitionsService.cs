using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalcJob = CalculateFunding.Common.ApiClient.Calcs.Models.Job;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.TemplateMetadata.Enums;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionsService : IDefinitionsService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IDatasetRepository _datasetsRepository;
        private readonly ISearchRepository<DatasetDefinitionIndex> _datasetDefinitionSearchRepository;
        private readonly AsyncPolicy _datasetDefinitionSearchRepositoryPolicy;
        private readonly AsyncPolicy _datasetsRepositoryPolicy;
        private readonly IExcelDatasetWriter _excelWriter;
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly IDefinitionChangesDetectionService _definitionChangesDetectionService;
        private readonly IMessengerService _messengerService;
        private readonly IPolicyRepository _policyRepository;
        private readonly IValidator<DatasetDefinition> _datasetDefinitionValidator;
        private readonly AsyncPolicy _calculationsResilience;
        private readonly IValidator<CreateDatasetDefinitionFromTemplateModel> _createDatasetDefinitionFromTemplateValidator;
        private readonly ICalculationsApiClient _calculations;

        public DefinitionsService(
            ILogger logger,
            IDatasetRepository dataSetsRepository,
            ISearchRepository<DatasetDefinitionIndex> datasetDefinitionSearchRepository,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IExcelDatasetWriter excelWriter,
            IBlobClient blobClient,
            IDefinitionChangesDetectionService definitionChangesDetectionService,
            IMessengerService messengerService,
            IPolicyRepository policyRepository,
            IValidator<DatasetDefinition> datasetDefinitionValidator,
            ICalculationsApiClient calculations,
            IValidator<CreateDatasetDefinitionFromTemplateModel> createDatasetDefinitionFromTemplateValidator)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(dataSetsRepository, nameof(dataSetsRepository));
            Guard.ArgumentNotNull(datasetDefinitionSearchRepository, nameof(datasetDefinitionSearchRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies, nameof(datasetsResiliencePolicies));
            Guard.ArgumentNotNull(excelWriter, nameof(excelWriter));
            Guard.ArgumentNotNull(definitionChangesDetectionService, nameof(definitionChangesDetectionService));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.DatasetDefinitionSearchRepository, nameof(datasetsResiliencePolicies.DatasetDefinitionSearchRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.DatasetRepository, nameof(datasetsResiliencePolicies.DatasetRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.BlobClient, nameof(datasetsResiliencePolicies.BlobClient));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(datasetDefinitionValidator, nameof(datasetDefinitionValidator));
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.CalculationsApiClient, nameof(datasetsResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(createDatasetDefinitionFromTemplateValidator, nameof(createDatasetDefinitionFromTemplateValidator));

            _logger = logger;
            _datasetsRepository = dataSetsRepository;
            _datasetDefinitionSearchRepository = datasetDefinitionSearchRepository;
            _datasetDefinitionSearchRepositoryPolicy = datasetsResiliencePolicies.DatasetDefinitionSearchRepository;
            _datasetsRepositoryPolicy = datasetsResiliencePolicies.DatasetRepository;
            _excelWriter = excelWriter;
            _blobClient = blobClient;
            _blobClientPolicy = datasetsResiliencePolicies.BlobClient;
            _definitionChangesDetectionService = definitionChangesDetectionService;
            _messengerService = messengerService;
            _policyRepository = policyRepository;
            _datasetDefinitionValidator = datasetDefinitionValidator;
            _calculations = calculations;
            _calculationsResilience = datasetsResiliencePolicies.CalculationsApiClient;
            _createDatasetDefinitionFromTemplateValidator = createDatasetDefinitionFromTemplateValidator;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_datasetsRepository).IsHealthOk();
            var searchRepoHealth = await _datasetDefinitionSearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(DefinitionsService)
            };
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _datasetDefinitionSearchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> SaveDefinition(string yaml, string yamlFilename, Reference user, string correlationId)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                string fileName = !string.IsNullOrEmpty(yamlFilename) ? yamlFilename : "File name not provided";

                _logger.Error($"Null or empty yaml provided for file: {fileName}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {fileName}");
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

            ValidationResult validationResult = await _datasetDefinitionValidator.ValidateAsync(definition);

            if (!validationResult.IsValid)
            {
                string errorMessage = $"Invalid metadata on definition. {validationResult}";

                _logger.Error(errorMessage);

                return new BadRequestObjectResult(errorMessage);
            }

            return await SaveDatasetDefinition(definition, correlationId, user);
        }

        private async Task<IEnumerable<IndexError>> IndexDatasetDefinition(DatasetDefinition definition, PoliciesApiModels.FundingStream fundingStream)
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
                Version = definition.Version,
                ModelHash = hashCode,
                FundingStreamId = fundingStream.Id,
                FundingStreamName = fundingStream.Name
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

        async public Task<IActionResult> GetDatasetDefinitions()
        {
            IEnumerable<DatasetDefinition> definitions = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDatasetDefinitions());

            return new OkObjectResult(definitions);
        }

        public async Task<IActionResult> GetDatasetDefinitionById(string datasetDefinitionId)
        {
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

        public async Task<IActionResult> GetDatasetDefinitionsByFundingStreamId(string fundingStreamId)
        {
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No FundingStreamId was provided to GetDatasetDefinitionByFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingStreamId provided");
            }

            IEnumerable<DatasetDefinationByFundingStream> defintions = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDatasetDefinitionsByFundingStreamId(fundingStreamId));

            if (defintions?.Any() == false)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(defintions);
        }

        public async Task<IActionResult> GetDatasetDefinitionsByIds(IEnumerable<string> definitionIds)
        {
            if (!definitionIds.Any())
            {
                _logger.Error($"No Dataset Definition Ids were provided to lookup");
                return new BadRequestObjectResult($"No DatasetDefinitionIds were provided to lookup");
            }

            IEnumerable<DatasetDefinition> defintions = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDatasetDefinitionsByQuery(d => definitionIds.Contains(d.Id)));
            return new OkObjectResult(definitionIds);
        }

        private async Task SaveToBlobStorage(byte[] excelfile, string definitionName)
        {
            string friendlyDefinitionName = definitionName.Replace("/", "_").Replace("\\", "_");

            ICloudBlob blob = _blobClient.GetBlockBlobReference($"schemas/{friendlyDefinitionName}.xlsx");

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(excelfile))
                {
                    await _blobClientPolicy.ExecuteAsync(() => blob.UploadFromStreamAsync(memoryStream));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to upload {definitionName} to blob storage");

                throw;
            }
        }

        public async Task<IActionResult> GetDatasetSchemaSasUrl(DatasetSchemaSasUrlRequestModel requestModel)
        {
            if (requestModel == null)
            {
                _logger.Warning("No dataset schema request model was provided");
                return new BadRequestObjectResult("No dataset schema request model was provided");
            }

            if (requestModel.DatasetDefinitionId.IsNullOrEmpty())
            {
                _logger.Warning("No dataset schema name was provided");
                return new BadRequestObjectResult("No dataset schema name was provided");
            }

            DatasetDefinition datasetDefinition = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDatasetDefinition(requestModel.DatasetDefinitionId));
            if (datasetDefinition == null)
            {
                return new NotFoundObjectResult("Data schema definiton not found");
            }

            string definitionName = datasetDefinition.Name.Replace("/", "_").Replace("\\", "_");

            string fileName = $"schemas/{definitionName}.xlsx";

            string blobUrl = _blobClient.GetBlobSasUrl(fileName, DateTimeOffset.UtcNow.AddDays(1), SharedAccessBlobPermissions.Read);

            return new OkObjectResult(new DatasetSchemaSasUrlResponseModel { SchemaUrl = blobUrl });
        }

        public async Task<IActionResult> CreateOrUpdateDatasetDefinition(CreateDatasetDefinitionFromTemplateModel model, string correlationId, Reference user)
        {
            ValidationResult validationResult = await _createDatasetDefinitionFromTemplateValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errorMessage = string.Join(";", validationResult.Errors.Select(x => x.ErrorMessage));
                _logger.Error(errorMessage);
                return new BadRequestObjectResult(errorMessage);
            };

            FundingStream fundingStream = await _policyRepository.GetFundingStream(model.FundingStreamId);
            TemplateMetadataDistinctCalculationsContents templateContents = await _policyRepository.GetDistinctTemplateMetadataCalculationsContents(model.FundingStreamId, model.FundingPeriodId, model.TemplateVersion);

            if (templateContents == null)
            {
                return new BadRequestObjectResult($"No funding template for given FundingStreamId " +
                    $"- {model.FundingStreamId}, FundingPeriodId - {model.FundingPeriodId}, TemplateVersion - {model.TemplateVersion}");
            }

            DatasetDefinition datasetDefinition = CreateDatasetDefinition(model, templateContents, fundingStream);

            return await SaveDatasetDefinition(datasetDefinition, correlationId, user);
        }

        private static DatasetDefinition CreateDatasetDefinition(
            CreateDatasetDefinitionFromTemplateModel model,
            TemplateMetadataDistinctCalculationsContents templateContent,
            FundingStream fundingStream)
        {
            int id = model.DatasetDefinitionId;
            string name = $"{fundingStream.Name}-{model.TemplateVersion}";
            DatasetDefinition datasetDefinition = new DatasetDefinition()
            {
                Id = id.ToString(),
                Version = model.Version,
                Name = name,
                Description = name,
                FundingStreamId = fundingStream.Id
            };

            id += 1;
            datasetDefinition.TableDefinitions = new List<TableDefinition>();
            TableDefinition tableDefinition = new TableDefinition
            {
                Id = id.ToString(),
                Name = name,
                FieldDefinitions = new List<FieldDefinition>()
            };

            id += 1;
            tableDefinition.FieldDefinitions.Add(new FieldDefinition()
            {
                Id = id.ToString(),
                Name = "UKPRN",
                IdentifierFieldType = IdentifierFieldType.UKPRN,
                Type = FieldType.String,
                Required = true
            });

            foreach (TemplateMetadataCalculation calculation in templateContent.Calculations)
            {
                id += 1;
                FieldDefinition fieldDefinition = new FieldDefinition()
                {
                    Id = id.ToString(),
                    Name = calculation.Name,
                    Required = false,
                    Type = GetFieldType(calculation.Type),
                    IsAggregable = calculation.AggregationType != AggregationType.None
                };

                tableDefinition.FieldDefinitions.Add(fieldDefinition);
            }

            datasetDefinition.TableDefinitions.Add(tableDefinition);

            return datasetDefinition;
        }

        private static FieldType GetFieldType(CalculationType calculationType)
        {
            switch (calculationType)
            {
                case CalculationType.Cash:
                case CalculationType.Rate:
                case CalculationType.PupilNumber:
                case CalculationType.Weighting:
                case CalculationType.PerPupilFunding:
                case CalculationType.LumpSum:
                case CalculationType.Number:
                    return FieldType.NullableOfDecimal;
                case CalculationType.Boolean:
                    return FieldType.Boolean;
                case CalculationType.Enum:
                    return FieldType.String;
                default:
                    return FieldType.String;
            }
        }

        private async Task<IActionResult> SaveDatasetDefinition(DatasetDefinition definition, string correlationId, Reference user)
        {
            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges();

            DatasetDefinition existingDefinition = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDatasetDefinition(definition.Id));

            // if version is null then use existing version
            definition.Version ??= existingDefinition?.Version;

            // if no existing version or version not set then default to version 1
            definition.Version ??= 1;

            IEnumerable<string> relationships = null;

            if (existingDefinition != null)
            {
                datasetDefinitionChanges = _definitionChangesDetectionService.DetectChanges(definition, existingDefinition);

                relationships = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.GetDistinctRelationshipSpecificationIdsForDatasetDefinitionId(datasetDefinitionChanges.Id));

                IEnumerable<FieldDefinitionChanges> fieldDefinitionChanges = datasetDefinitionChanges.TableDefinitionChanges.SelectMany(m => m.FieldChanges);

                if (!relationships.IsNullOrEmpty() && !fieldDefinitionChanges.IsNullOrEmpty())
                {
                    if (fieldDefinitionChanges.Any(m => m.ChangeTypes.Any(c => c == FieldDefinitionChangeType.RemovedField)))
                    {
                        return new BadRequestObjectResult("Unable to remove a field as there are currently relationships setup against this schema");
                    }

                    if (fieldDefinitionChanges.Any(m => m.ChangeTypes.Any(c => c == FieldDefinitionChangeType.IdentifierType)))
                    {
                        return new BadRequestObjectResult("Unable to change provider identifier as there are currently relationships setup against this schema");
                    }
                }
            }

            try
            {
                HttpStatusCode result = await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetsRepository.SaveDefinition(definition));
                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    _logger.Error($"Failed to save dataset definition - {definition.Name} to cosmos db with status {statusCode}");

                    return new StatusCodeResult(statusCode);
                }

                IEnumerable<PoliciesApiModels.FundingStream> fundingStreams = await _policyRepository.GetFundingStreams();
                PoliciesApiModels.FundingStream fundingStream = fundingStreams.SingleOrDefault(_ => _.Id == definition.FundingStreamId);

                await IndexDatasetDefinition(definition, fundingStream);
            }
            catch (Exception exception)
            {
                string errorMessage = $"Exception occurred writing dataset definition - {definition.Name} to cosmos db";
                _logger.Error(exception, errorMessage);
                return new InternalServerErrorResult(errorMessage);
            }

            byte[] excelAsBytes = _excelWriter.Write(definition);

            if (excelAsBytes == null || excelAsBytes.Length == 0)
            {
                string errorMessage = $"Failed to generate excel file for {definition.Name}";
                _logger.Error(errorMessage);
                return new InternalServerErrorResult(errorMessage);
            }

            try
            {
                await SaveToBlobStorage(excelAsBytes, definition.Name);
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }

            _logger.Information($"Successfully saved dataset definition - {definition.Name} to cosmos db");

            if (existingDefinition != null && datasetDefinitionChanges.HasChanges)
            {
                if (!relationships.IsNullOrEmpty())
                {
                    Task<ApiResponse<CalcJob>>[] updateCodeContextJobs =
                        relationships.Select(specificationId => _calculationsResilience.ExecuteAsync(() =>
                            _calculations.QueueCodeContextUpdate(specificationId))).ToArray();

                    await TaskHelper.WhenAllAndThrow(updateCodeContextJobs);
                }

                IDictionary<string, string> properties = MessageExtensions.BuildMessageProperties(correlationId, user);

                await _messengerService.SendToTopic(ServiceBusConstants.TopicNames.DataDefinitionChanges, datasetDefinitionChanges, properties);
            }

            return new OkResult();
        }
    }
}
