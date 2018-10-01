using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using OfficeOpenXml;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetService : IDatasetService, IHealthChecker
    {
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        private readonly IDatasetRepository _datasetRepository;
        private readonly IValidator<CreateNewDatasetModel> _createNewDatasetModelValidator;
        private readonly IValidator<DatasetVersionUpdateModel> _datasetVersionUpdateModelValidator;
        private readonly IMapper _mapper;
        private readonly IValidator<DatasetMetadataModel> _datasetMetadataModelValidator;
        private readonly ISearchRepository<DatasetIndex> _searchRepository;
        private readonly IValidator<GetDatasetBlobModel> _getDatasetBlobModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IExcelDatasetReader _excelDatasetReader;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICalcsRepository _calcsRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly IProvidersResultsRepository _providersResultsRepository;
        private readonly ITelemetry _telemetry;
        private readonly IValidator<ExcelPackage> _dataWorksheetValidator;
        private readonly Policy _providerResultsRepositoryPolicy;
        private readonly IValidator<DatasetUploadValidationModel> _datasetUploadValidator;

        static IEnumerable<ProviderSummary> _providerSummaries = new List<ProviderSummary>();
        private readonly IVersionRepository<ProviderSourceDatasetVersion> _sourceDatasetsVersionRepository;

        public DatasetService(IBlobClient blobClient,
            ILogger logger,
            IDatasetRepository datasetRepository,
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator,
            IValidator<DatasetVersionUpdateModel> datasetVersionUpdateModelValidator,
            IMapper mapper,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator,
            ISearchRepository<DatasetIndex> searchRepository,
            IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator,
            ISpecificationsRepository specificationsRepository,
            IMessengerService messengerService,
            IExcelDatasetReader excelDatasetReader,
            ICacheProvider cacheProvider,
            ICalcsRepository calcsRepository,
            IProviderRepository providerRepository,
            IProvidersResultsRepository providersResultsRepository,
            ITelemetry telemetry,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IValidator<ExcelPackage> dataWorksheetValidator,
            IValidator<DatasetUploadValidationModel> datasetUploadValidator,
            IVersionRepository<ProviderSourceDatasetVersion> sourceDatasetsVersionRepository)
        {
            _blobClient = blobClient;
            _logger = logger;
            _datasetRepository = datasetRepository;
            _createNewDatasetModelValidator = createNewDatasetModelValidator;
            _datasetVersionUpdateModelValidator = datasetVersionUpdateModelValidator;
            _mapper = mapper;
            _datasetMetadataModelValidator = datasetMetadataModelValidator;
            _searchRepository = searchRepository;
            _getDatasetBlobModelValidator = getDatasetBlobModelValidator;
            _messengerService = messengerService;
            _specificationsRepository = specificationsRepository;
            _excelDatasetReader = excelDatasetReader;
            _cacheProvider = cacheProvider;
            _calcsRepository = calcsRepository;
            _providerRepository = providerRepository;
            _providersResultsRepository = providersResultsRepository;
            _telemetry = telemetry;
            _dataWorksheetValidator = dataWorksheetValidator;
            _datasetUploadValidator = datasetUploadValidator;

            Guard.ArgumentNotNull(datasetsResiliencePolicies, nameof(datasetsResiliencePolicies));

            _providerResultsRepositoryPolicy = datasetsResiliencePolicies.ProviderResultsRepository;
            _sourceDatasetsVersionRepository = sourceDatasetsVersionRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var blobHealth = await _blobClient.IsHealthOk();
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_datasetRepository).IsHealthOk();
            var searchRepoHealth = await _searchRepository.IsHealthOk();
            string queueName = ServiceBusConstants.QueueNames.CalculationJobInitialiser;
            var messengerServiceHealth = await _messengerService.IsHealthOk(queueName);
            var cacheHealth = await _cacheProvider.IsHealthOk();
            ServiceHealth providersResultsRepoHealth = await ((IHealthChecker)_providersResultsRepository).IsHealthOk();
            ServiceHealth providerRepoHealth = await ((IHealthChecker)_providerRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(DatasetService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message });
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = messengerServiceHealth.Ok, DependencyName = $"{_messengerService.GetType().GetFriendlyName()} for queue: {queueName}", Message = messengerServiceHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });
            health.Dependencies.AddRange(providersResultsRepoHealth.Dependencies);
            health.Dependencies.AddRange(providerRepoHealth.Dependencies);

            return health;
        }

        async public Task<IActionResult> CreateNewDataset(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CreateNewDatasetModel model = JsonConvert.DeserializeObject<CreateNewDatasetModel>(json);

            if (model == null)
            {
                _logger.Error("Null model name was provided to CreateNewDataset");
                return new BadRequestObjectResult("Null model name was provided");
            }
            var validationResult = (await _createNewDatasetModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            string version = "v1";

            string datasetId = Guid.NewGuid().ToString();

            string fileName = $"{datasetId}/{version}/{model.Filename}";

            string blobUrl = _blobClient.GetBlobSasUrl(fileName,
                DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);

            NewDatasetVersionResponseModel responseModel = _mapper.Map<NewDatasetVersionResponseModel>(model);

            responseModel.DatasetId = datasetId;
            responseModel.BlobUrl = blobUrl;
            responseModel.Author = request.GetUserOrDefault();
            responseModel.DefinitionId = model.DefinitionId;

            return new OkObjectResult(responseModel);
        }

        public async Task<IActionResult> DatasetVersionUpdate(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            DatasetVersionUpdateModel model = JsonConvert.DeserializeObject<DatasetVersionUpdateModel>(json);

            if (model == null)
            {
                _logger.Warning($"Null model was provided to {nameof(DatasetVersionUpdate)}");
                return new BadRequestObjectResult("Null model name was provided");
            }
            var validationResult = (await _datasetVersionUpdateModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(model.DatasetId);
            if (dataset == null)
            {
                _logger.Warning("Dataset was not found with ID {datasetId} when trying to add new dataset version", model.DatasetId);

                return new PreconditionFailedResult($"Dataset was not found with ID {model.DatasetId} when trying to add new dataset version");
            }

            int nextVersion = dataset.GetNextVersion();

            string version = $"{nextVersion}";

            string fileName = $"{dataset.Id}/v{version}/{model.Filename}";

            string blobUrl = _blobClient.GetBlobSasUrl(fileName,
                DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);

            NewDatasetVersionResponseModel responseModel = _mapper.Map<NewDatasetVersionResponseModel>(model);

            responseModel.DatasetId = dataset.Id;
            responseModel.BlobUrl = blobUrl;
            responseModel.Author = request.GetUser();
            responseModel.DefinitionId = dataset.Definition.Id;
            responseModel.Name = dataset.Name;
            responseModel.Description = dataset.Description;
            responseModel.Version = nextVersion;

            return new OkObjectResult(responseModel);
        }

        async public Task<IActionResult> GetDatasetByName(HttpRequest request)
        {
            request.Query.TryGetValue("datasetName", out var dsName);

            var datasetName = dsName.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(datasetName))
            {
                _logger.Error("No dataset name was provided to GetDatasetByName");

                return new BadRequestObjectResult("Null or empty dataset name provided");
            }

            IEnumerable<Dataset> datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Name.ToLower() == datasetName.ToLower());

            if (!datasets.Any())
            {
                _logger.Information($"Dataset was not found for name: {datasetName}");

                return new NotFoundResult();
            }

            _logger.Information($"Dataset found for name: {datasetName}");

            return new OkObjectResult(datasets.FirstOrDefault());
        }

        async public Task<IActionResult> GetDatasetsByDefinitionId(HttpRequest request)
        {
            request.Query.TryGetValue("definitionId", out var definitionId);

            var datasetDefinitionId = definitionId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(datasetDefinitionId))
            {
                _logger.Error($"No {nameof(definitionId)} was provided to {nameof(GetDatasetsByDefinitionId)}");

                return new BadRequestObjectResult($"Null or empty {nameof(definitionId)} provided");
            }

            IEnumerable<Dataset> datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Definition.Id == datasetDefinitionId.ToLower());

            IEnumerable<DatasetViewModel> result = datasets?.Select(_mapper.Map<DatasetViewModel>).ToArraySafe();

            return new OkObjectResult(result ?? Enumerable.Empty<DatasetViewModel>());
        }

        public async Task<IActionResult> GetValidateDatasetStatus(HttpRequest request)
        {
            request.Query.TryGetValue("operationId", out var operationIdParse);

            string operationId = operationIdParse.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(operationId))
            {
                _logger.Error($"No {nameof(operationIdParse)} was provided to {nameof(GetValidateDatasetStatus)}");

                return new BadRequestObjectResult($"Null or empty {nameof(operationIdParse)} provided");
            }

            DatasetValidationStatusModel status = await _cacheProvider.GetAsync<DatasetValidationStatusModel>($"{CacheKeys.DatasetValidationStatus}:{operationId}");
            if (status == null)
            {
                return new NotFoundObjectResult("Unable to find Dataset Validation Status");
            }

            return new OkObjectResult(status);
        }

        public async Task<IActionResult> ValidateDataset(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            GetDatasetBlobModel model = JsonConvert.DeserializeObject<GetDatasetBlobModel>(json);
            if (model == null)
            {
                _logger.Error("Null model name was provided to ValidateDataset");
                return new BadRequestObjectResult("Null model name was provided");
            }

            ValidationResult validationResult = (await _getDatasetBlobModelValidator.ValidateAsync(model));

            if (validationResult != null && (!validationResult.IsValid || validationResult.Errors.Count > 0))
            {
                _logger.Error($"{nameof(GetDatasetBlobModel)} model error: {0}", validationResult.Errors);

                return validationResult.PopulateModelState();
            }

            string fullBlobName = model.ToString();

            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);

            if (blob == null)
            {
                _logger.Error($"Failed to find blob with path: {fullBlobName}");
                return new PreconditionFailedResult($"Failed to find blob with path: {fullBlobName}");
            }

            await blob.FetchAttributesAsync();
            Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob);

            if (datasetStream == null || datasetStream.Length == 0)
            {
                _logger.Error($"Blob {blob.Name} contains no data");
                return new PreconditionFailedResult($"Blob {blob.Name} contains no data");
            }

            string dataDefinitionId = blob.Metadata["dataDefinitionId"];


            DatasetDefinition datasetDefinition =
                (await _datasetRepository.GetDatasetDefinitionsByQuery(m => m.Id == dataDefinitionId)).FirstOrDefault();

            if (datasetDefinition == null)
            {
                _logger.Error($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");

                return new PreconditionFailedResult($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");
            }

            DatasetValidationStatusModel responseModel = new DatasetValidationStatusModel()
            {
                OperationId = Guid.NewGuid().ToString(),
            };

            if (blob.Metadata.ContainsKey("datasetId"))
            {
                responseModel.DatasetId = blob.Metadata["datasetId"];
                model.DatasetId = blob.Metadata["datasetId"];
            }

            IDictionary<string, string> messageProperties = CreateMessageProperties(request);
            messageProperties.Add("operation-id", responseModel.OperationId);

            await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.ValidateDataset, model, messageProperties);

            await _cacheProvider.SetAsync<DatasetValidationStatusModel>($"{CacheKeys.DatasetValidationStatus}:{responseModel.OperationId}", responseModel);

            return new OkObjectResult(responseModel);
        }

        public async Task ValidateDataset(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string operationId = null;
            if (message.UserProperties.ContainsKey("operation-id"))
            {
                operationId = message.UserProperties["operation-id"]?.ToString();
            }

            if (string.IsNullOrWhiteSpace(operationId))
            {
                _logger.Error($"Operation ID was null or empty string on the message from {nameof(ValidateDataset)}");

                return;
            }

            await SetValidationStatus(operationId, DatasetValidationStatus.Processing);

            GetDatasetBlobModel model = message.GetPayloadAsInstanceOf<GetDatasetBlobModel>();
            if (model == null)
            {
                _logger.Error("Null model was provided to ValidateDataset");

                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, "Null model was provided to ValidateDataset");

                return;
            }

            ValidationResult validationResult = (await _getDatasetBlobModelValidator.ValidateAsync(model));
            if (validationResult == null)
            {
                _logger.Error($"{nameof(GetDatasetBlobModel)} validation result returned null");
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, $"{nameof(GetDatasetBlobModel)} validation result returned null");

                return;
            }
            else if (!validationResult.IsValid || validationResult.Errors.Count > 0)
            {
                _logger.Error($"{nameof(GetDatasetBlobModel)} model error: {{0}}", validationResult.Errors);
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, $"{nameof(GetDatasetBlobModel)} model error", ConvertToErrorDictionary(validationResult));

                return;
            }

            string fullBlobName = model.ToString();

            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);
            if (blob == null)
            {
                _logger.Error($"Failed to find blob with path: {fullBlobName}");

                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, $"Failed to find blob with path: {fullBlobName}");
                return;
            }

            await blob.FetchAttributesAsync();

            using (Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob))
            {
                if (datasetStream == null || datasetStream.Length == 0)
                {
                    _logger.Error($"Blob {blob.Name} contains no data");

                    await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, $"Blob {blob.Name} contains no data");
                    return;
                }

                try
                {
                    await SetValidationStatus(operationId, DatasetValidationStatus.ValidatingExcelWorkbook);

                    using (ExcelPackage excel = new ExcelPackage(datasetStream))
                    {
                        validationResult = _dataWorksheetValidator.Validate(excel);

                        if (validationResult != null && (!validationResult.IsValid || validationResult.Errors.Count > 0))
                        {
                            await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, null, ConvertToErrorDictionary(validationResult));
                            return;
                        }
                    }
                }
                catch (Exception exception)
                {
                    const string errorMessage = "The data source file type is invalid. Check that your file is an xls or xlsx file";

                    _logger.Error(exception, errorMessage);

                    validationResult = new ValidationResult();
                    validationResult.Errors.Add(new ValidationFailure("typical-model-validation-error", string.Empty));
                    validationResult.Errors.Add(new ValidationFailure(nameof(model.Filename), errorMessage));

                    await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, null, ConvertToErrorDictionary(validationResult));

                    return;
                }

                string dataDefinitionId = blob.Metadata["dataDefinitionId"];
                DatasetDefinition datasetDefinition =
                    (await _datasetRepository.GetDatasetDefinitionsByQuery(m => m.Id == dataDefinitionId)).FirstOrDefault();

                if (datasetDefinition == null)
                {
                    string errorMessage = $"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}";
                    _logger.Error(errorMessage);

                    await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage);
                    return;
                }

                await SetValidationStatus(operationId, DatasetValidationStatus.ValidatingTableResults);

                (IDictionary<string, IEnumerable<string>> validationFailures, int rowCount) = await ValidateTableResults(datasetDefinition, blob);

                if (validationFailures.Count == 0)
                {
                    try
                    {
                        DatasetCreateUpdateResponseModel datasetCreateUpdateResponseModel = new DatasetCreateUpdateResponseModel
                        {
                            CurrentRowCount = rowCount
                        };

                        await SetValidationStatus(operationId, DatasetValidationStatus.SavingResults);

                        Dataset dataset;

                        if (model.Version == 1)
                        {
                            dataset = await SaveNewDatasetAndVersion(blob, datasetDefinition, rowCount);
                        }
                        else
                        {
                            Reference user = message.GetUserDetails();

                            dataset = await UpdateExistingDatasetAndAddVersion(blob, model, user, rowCount);
                        }

                        await SetValidationStatus(operationId, DatasetValidationStatus.Validated, datasetId: dataset.Id);
                    }
                    catch (Exception exception)
                    {
                        _logger.Error(exception, "Failed to save the dataset or dataset version during validation");

                        await SetValidationStatus(operationId, DatasetValidationStatus.ExceptionThrown, exception.Message);
                        throw;
                    }
                }
                else
                {
                    await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, validationFailures: validationFailures);
                }
            }
        }

        private IDictionary<string, IEnumerable<string>> ConvertToErrorDictionary(ValidationResult validationResult)
        {
            Dictionary<string, IEnumerable<string>> result = new Dictionary<string, IEnumerable<string>>();

            if (validationResult != null)
            {
                foreach (ValidationFailure validationFailure in validationResult.Errors)
                {
                    List<string> errorMessages;
                    if (!result.ContainsKey(validationFailure.PropertyName))
                    {
                        errorMessages = new List<string>();
                        result.Add(validationFailure.PropertyName, errorMessages);
                    }
                    else
                    {
                        errorMessages = result[validationFailure.PropertyName] as List<string>;
                    }

                    errorMessages.Add(validationFailure.ErrorMessage);
                }
            }

            return result;
        }

        private async Task SetValidationStatus(string operationId, DatasetValidationStatus currentOperation, string errorMessage = null, IDictionary<string, IEnumerable<string>> validationFailures = null, string datasetId = null)
        {
            DatasetValidationStatusModel status = new DatasetValidationStatusModel()
            {
                OperationId = operationId,
                CurrentOperation = currentOperation,
                ErrorMessage = errorMessage,
                ValidationFailures = validationFailures,
                LastUpdated = DateTimeOffset.Now,
                DatasetId = datasetId,
            };

            await _cacheProvider.SetAsync<DatasetValidationStatusModel>($"{CacheKeys.DatasetValidationStatus}:{status.OperationId}", status);
        }

        async public Task<IActionResult> ProcessDataset(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            Dataset dataset = JsonConvert.DeserializeObject<Dataset>(json);

            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"No {nameof(specificationId)}");

                return new BadRequestObjectResult($"Null or empty {nameof(specificationId)} provided");
            }

            request.Query.TryGetValue("relationshipId", out var relId);

            var relationshipId = relId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                _logger.Error($"No {nameof(relationshipId)}");

                return new BadRequestObjectResult($"Null or empty {nameof(relationshipId)} provided");
            }

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (relationship == null)
            {
                _logger.Error($"Relationship not found for relationship id: {relationshipId}");
                throw new ArgumentNullException(nameof(relationshipId), "A null or empty relationship returned from repository");
            }

            BuildProject buildProject = null;

            Reference user = request.GetUser();

            try
            {
                buildProject = await ProcessDataset(dataset, specificationId, relationshipId, relationship.DatasetVersion.Version, user);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Failed to process data with exception: {exception.Message}");
            }

            if (buildProject != null && !buildProject.DatasetRelationships.IsNullOrEmpty() && buildProject.DatasetRelationships.Any(m => m.DefinesScope))
            {
                Message message = new Message();

                IDictionary<string, string> messageProperties = message.BuildMessageProperties();
                messageProperties.Add("specification-id", specificationId);
                messageProperties.Add("provider-cache-key", $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}");

                await _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.CalculationJobInitialiser,
                        null, messageProperties);

                _telemetry.TrackEvent("InstructCalculationAllocationEventRun",
                      new Dictionary<string, string>()
                      {
                            { "specificationId" , buildProject.SpecificationId },
                            { "buildProjectId" , buildProject.Id },
                            { "datasetId", dataset.Id }
                      },
                      new Dictionary<string, double>()
                      {
                            { "InstructCalculationAllocationEventRunDataset" , 1 },
                            { "InstructCalculationAllocationEventRun" , 1 }
                      }
                );
            }

            return new OkResult();
        }

        async public Task ProcessDataset(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            IDictionary<string, object> properties = message.UserProperties;

            Dataset dataset = message.GetPayloadAsInstanceOf<Dataset>();

            if (dataset == null)
            {
                _logger.Error("A null dataset was provided to ProcessData");

                throw new ArgumentNullException(nameof(dataset), "A null dataset was provided to ProcessDataset");
            }

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("Specification Id key is missing in ProcessDataset message properties");
                throw new KeyNotFoundException("Specification Id key is missing in ProcessDataset message properties");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("A null or empty specification id was provided to ProcessData");

                throw new ArgumentNullException(nameof(specificationId), "A null or empty specification id was provided to ProcessData");
            }

            if (!message.UserProperties.ContainsKey("relationship-id"))
            {
                _logger.Error("Relationship Id key is missing in ProcessDataset message properties");
                throw new KeyNotFoundException("Relationship Id key is missing in ProcessDataset message properties");
            }

            string relationshipId = message.UserProperties["relationship-id"].ToString();
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                _logger.Error("A null or empty relationship id was provided to ProcessDataset");

                throw new ArgumentNullException(nameof(specificationId), "A null or empty relationship id was provided to ProcessData");
            }

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (relationship == null)
            {
                _logger.Error($"Relationship not found for relationship id: {relationshipId}");
                throw new ArgumentNullException(nameof(relationshipId), "A null or empty relationship returned from repository");
            }

            BuildProject buildProject = null;

            Reference user = message.GetUserDetails();

            try
            {
                buildProject = await ProcessDataset(dataset, specificationId, relationshipId, relationship.DatasetVersion.Version, user);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Failed to run ProcessDataset with exception: {exception.Message} for relationship ID '{relationshipId}'");
                throw;
            }

            if (buildProject != null && !buildProject.DatasetRelationships.IsNullOrEmpty() && buildProject.DatasetRelationships.Any(m => m.DefinesScope))
            {
                IDictionary<string, string> messageProperties = message.BuildMessageProperties();
                messageProperties.Add("specification-id", specificationId);
                messageProperties.Add("provider-cache-key", $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}");

                await _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.CalculationJobInitialiser,
                        null, messageProperties);

                _telemetry.TrackEvent("InstructCalculationAllocationEventRun",
                      new Dictionary<string, string>()
                      {
                            { "specificationId" , buildProject.SpecificationId },
                            { "buildProjectId" , buildProject.Id },
                            { "datasetId", dataset.Id }
                      },
                      new Dictionary<string, double>()
                      {
                            { "InstructCalculationAllocationEventRunDataset" , 1 },
                            { "InstructCalculationAllocationEventRun" , 1 }
                      }
                );
            }
        }

        public async Task<IActionResult> DownloadDatasetFile(HttpRequest request)
        {
            request.Query.TryGetValue("datasetId", out var datasetId);

            var currentDatasetId = datasetId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(currentDatasetId))
            {
                _logger.Error($"No {nameof(currentDatasetId)} was provided to {nameof(DownloadDatasetFile)}");

                return new BadRequestObjectResult($"Null or empty {nameof(currentDatasetId)} provided");
            }

            Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(currentDatasetId);

            if (dataset == null)
            {
                _logger.Error($"A dataset could not be found for dataset id: {currentDatasetId}");

                return new StatusCodeResult(412);
            }

            string fullBlobName = dataset.Current?.BlobName;

            if (string.IsNullOrWhiteSpace(fullBlobName))
            {
                _logger.Error($"A blob name could not be found for dataset id: {currentDatasetId}");

                return new StatusCodeResult(412);
            }

            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);

            if (blob == null)
            {
                _logger.Error($"Failed to find blob with path: {fullBlobName}");
                return new NotFoundResult();
            }

            string blobUrl = _blobClient.GetBlobSasUrl(fullBlobName, DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read);

            DatasetDownloadModel downloadModel = new DatasetDownloadModel { Url = blobUrl };

            return new OkObjectResult(downloadModel);
        }

        public async Task<IActionResult> Reindex(HttpRequest request)
        {
            IEnumerable<DocumentEntity<Dataset>> datasets = await _datasetRepository.GetDatasets();
            int searchBatchSize = 100;

            int totalInserts = 0;

            List<DatasetIndex> searchEntries = new List<DatasetIndex>(searchBatchSize);

            foreach (DocumentEntity<Dataset> dataset in datasets)
            {
                DatasetIndex datasetIndex = new DatasetIndex()
                {
                    DefinitionId = dataset.Content.Definition.Id,
                    DefinitionName = dataset.Content.Definition.Name,
                    Id = dataset.Content.Id,
                    LastUpdatedDate = dataset.UpdatedAt,
                    Name = dataset.Content.Name,
                    Status = Enum.GetName(typeof(PublishStatus), dataset.Content.Current.PublishStatus),
                    Description = dataset.Content.Description,
                    Version = dataset.Content.Current.Version,
                };

                searchEntries.Add(datasetIndex);

                if (searchEntries.Count >= searchBatchSize)
                {
                    await _searchRepository.Index(searchEntries);
                    totalInserts += searchEntries.Count;
                    searchEntries.Clear();
                }
            }

            if (searchEntries.Any())
            {
                totalInserts += searchEntries.Count;
                await _searchRepository.Index(searchEntries);
            }

            return new OkObjectResult($"Indexed total of {totalInserts} Datasets");
        }

        public async Task<IActionResult> RegenerateProviderSourceDatasets(HttpRequest httpRequest)
        {
            httpRequest.Query.TryGetValue("specificationId", out var specificationIdValues);

            string specificationId = specificationIdValues.FirstOrDefault();

            IEnumerable<DefinitionSpecificationRelationship> relationships;

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                relationships = await _datasetRepository.GetAllDefinitionSpecificationsRelationships(); ;
            }
            else
            {
                relationships = await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(r => r.Specification.Id == specificationId);
            }

            Dictionary<string, Dataset> datasets = new Dictionary<string, Dataset>();

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                Dataset dataset;

                if (relationship == null || relationship.DatasetVersion == null || string.IsNullOrWhiteSpace(relationship.DatasetVersion.Id))
                {
                    continue;
                }

                if (!datasets.TryGetValue(relationship.DatasetVersion.Id, out dataset))
                {
                    dataset = (await _datasetRepository.GetDatasetsByQuery(c => c.Id == relationship.DatasetVersion.Id)).FirstOrDefault();
                    datasets.Add(relationship.DatasetVersion.Id, dataset);
                }

                IDictionary<string, string> properties = httpRequest.BuildMessageProperties();

                properties.Add("specification-id", relationship.Specification.Id);
                properties.Add("relationship-id", relationship.Id);

                await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.ProcessDataset, dataset, properties);

            }

            return new OkObjectResult(relationships);
        }

        private static IEnumerable<string> GetProviderIdsForIdentifier(DatasetDefinition datasetDefinition, RowLoadResult row)
        {
            IEnumerable<FieldDefinition> identifierFields = datasetDefinition.TableDefinitions?.First().FieldDefinitions.Where(x => x.IdentifierFieldType.HasValue);

            foreach (FieldDefinition field in identifierFields)
            {
                if (!string.IsNullOrWhiteSpace(field.Name))
                {
                    if (row.Fields.ContainsKey(field.Name))
                    {
                        string identifier = row.Fields[field.Name]?.ToString();
                        if (!string.IsNullOrWhiteSpace(identifier))
                        {
                            Dictionary<string, List<string>> lookup = GetDictionaryForIdentifierType(field.IdentifierFieldType, identifier);
                            if (lookup.TryGetValue(identifier, out List<string> providerIds))
                            {
                                return providerIds;
                            }
                        }
                        else
                        {
                            // For debugging only
                            //_logger.Debug("Found identifier with null or emtpy string for provider");
                        }
                    }
                }
            }

            return new string[0];
        }

        /// <summary>
        /// Gets list of Provider IDs from the given Identifier Type and Identifier Value
        /// </summary>
        /// <param name="identifierFieldType">Identifier Type</param>
        /// <param name="fieldIdentifierValue">Identifier ID - eg UPIN value</param>
        /// <returns>List of Provider IDs matching the given identifiers</returns>
        private static Dictionary<string, List<string>> GetDictionaryForIdentifierType(IdentifierFieldType? identifierFieldType, string fieldIdentifierValue)
        {
            if (!identifierFieldType.HasValue)
            {
                return new Dictionary<string, List<string>>();
            }

            // Expression to filter ProviderSummaries - this selects which field on the ProviderSummary to filter on, eg UPIN
            Func<ProviderSummary, string> identifierSelectorExpression = GetIdentifierSelectorExpression(identifierFieldType.Value);

            // Find ProviderIds from the list of all providers - given the field and value of the ID
            IEnumerable<string> filteredIdentifiers = _providerSummaries.Where(x => identifierSelectorExpression(x) == fieldIdentifierValue).Select(m => m.Id);

            return new Dictionary<string, List<string>> { { fieldIdentifierValue, filteredIdentifiers.ToList() } };
        }

        private async Task<Dataset> SaveNewDatasetAndVersion(ICloudBlob blob, DatasetDefinition datasetDefinition, int rowCount)
        {
            Guard.ArgumentNotNull(blob, nameof(blob));
            Guard.ArgumentNotNull(datasetDefinition, nameof(datasetDefinition));
            Guard.ArgumentNotNull(rowCount, nameof(rowCount));

            IDictionary<string, string> metadata = blob.Metadata;

            Guard.ArgumentNotNull(metadata, nameof(metadata));

            DatasetMetadataModel metadataModel = new DatasetMetadataModel(metadata);

            var validationResult = await _datasetMetadataModelValidator.ValidateAsync(metadataModel);

            if (!validationResult.IsValid)
            {
                _logger.Error($"Invalid metadata on blob: {blob.Name}");

                throw new Exception($"Invalid metadata on blob: {blob.Name}");
            }

            DatasetVersion newVersion = new DatasetVersion
            {
                Author = new Reference(metadataModel.AuthorId, metadataModel.AuthorName),
                Version = 1,
                Date = DateTimeOffset.Now,
                PublishStatus = PublishStatus.Draft,
                BlobName = blob.Name,
                RowCount = rowCount,
            };

            Dataset dataset = new Dataset
            {
                Id = metadataModel.DatasetId,
                Name = metadataModel.Name,
                Description = metadataModel.Description,
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Current = newVersion,
                History = new List<DatasetVersion>
                {
                    newVersion
                }
            };

            HttpStatusCode statusCode = await _datasetRepository.SaveDataset(dataset);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save dataset for id: {metadataModel.DatasetId} with status code {statusCode.ToString()}");

                throw new InvalidOperationException($"Failed to save dataset for id: {metadataModel.DatasetId} with status code {statusCode.ToString()}");
            }

            IEnumerable<IndexError> indexErrors = await IndexDatasetInSearch(dataset);

            if (indexErrors.Any())
            {
                string errors = string.Join(";", indexErrors.Select(m => m.ErrorMessage).ToArraySafe());

                _logger.Error($"Failed to save dataset for id: {metadataModel.DatasetId} in search with errors {errors}");

                throw new InvalidOperationException($"Failed to save dataset for id: {metadataModel.DatasetId} in search with errors {errors}");
            }

            return dataset;
        }

        private async Task<Dataset> UpdateExistingDatasetAndAddVersion(ICloudBlob blob, GetDatasetBlobModel model, Reference author, int rowCount)
        {
            Guard.ArgumentNotNull(blob, nameof(blob));

            IDictionary<string, string> metadata = blob.Metadata;

            Guard.ArgumentNotNull(metadata, nameof(metadata));

            Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(model.DatasetId);
            if (dataset == null)
            {
                _logger.Warning($"Failed to retrieve dataset for id: {model.DatasetId} response was null");

                throw new InvalidOperationException($"Failed to retrieve dataset for id: {model.DatasetId} response was null");
            }

            if (model.Version != dataset.GetNextVersion())
            {
                _logger.Error($"Failed to save dataset or dataset version for id: {model.DatasetId} due to version mismatch. Expected next version to be {dataset.GetNextVersion()} but request provided '{model.Version}'");

                throw new InvalidOperationException($"Failed to save dataset or dataset version for id: {model.DatasetId} due to version mismatch. Expected next version to be {dataset.GetNextVersion()} but request provided '{model.Version}'");
            }

            DatasetVersion newVersion = new DatasetVersion
            {
                Author = new Reference(author.Id, author.Name),
                Version = model.Version,
                Date = DateTimeOffset.Now,
                PublishStatus = PublishStatus.Draft,
                BlobName = blob.Name,
                Commment = model.Comment,
                RowCount = rowCount,
            };

            dataset.Description = model.Description;
            dataset.Current = newVersion;
            dataset.History.Add(newVersion);

            HttpStatusCode statusCode = await _datasetRepository.SaveDataset(dataset);

            if (!statusCode.IsSuccess())
            {
                _logger.Warning($"Failed to save dataset for id: {model.DatasetId} with status code {statusCode.ToString()}");

                throw new InvalidOperationException($"Failed to save dataset for id: {model.DatasetId} with status code {statusCode.ToString()}");
            }

            IEnumerable<IndexError> indexErrors = await IndexDatasetInSearch(dataset);

            if (indexErrors.Any())
            {
                string errors = string.Join(";", indexErrors.Select(m => m.ErrorMessage).ToArraySafe());

                _logger.Warning($"Failed to save dataset for id: {model.DatasetId} in search with errors {errors}");

                throw new InvalidOperationException($"Failed to save dataset for id: {model.DatasetId} in search with errors {errors}");
            }

            return dataset;
        }

        private Task<IEnumerable<IndexError>> IndexDatasetInSearch(Dataset dataset)
        {
            Guard.ArgumentNotNull(dataset, nameof(dataset));

            return _searchRepository.Index(new List<DatasetIndex>
            {
                new DatasetIndex
                {
                    Id = dataset.Id,
                    Name = dataset.Name,
                    DefinitionId = dataset.Definition.Id,
                    DefinitionName = dataset.Definition.Name,
                    Status = dataset.Current.PublishStatus.ToString(),
                    LastUpdatedDate = DateTimeOffset.Now,
                    Description = dataset.Description,
                    Version = dataset.Current.Version,
                }
            });
        }

        private static Func<ProviderSummary, string> GetIdentifierSelectorExpression(IdentifierFieldType identifierFieldType)
        {
            if (identifierFieldType == IdentifierFieldType.URN)
            {
                return x => x.URN;
            }
            else if (identifierFieldType == IdentifierFieldType.Authority)
            {
                return x => x.Authority;
            }
            else if (identifierFieldType == IdentifierFieldType.EstablishmentNumber)
            {
                return x => x.EstablishmentNumber;
            }
            else if (identifierFieldType == IdentifierFieldType.UKPRN)
            {
                return x => x.UKPRN;
            }
            else if (identifierFieldType == IdentifierFieldType.UPIN)
            {
                return x => x.UPIN;
            }
            else
            {
                return null;
            }
        }

        private async Task<(IDictionary<string, IEnumerable<string>> validationFailures, int providersProcessed)> ValidateTableResults(DatasetDefinition datasetDefinition, ICloudBlob blob)
        {
            int rowCount = 0;
            if (_providerSummaries.IsNullOrEmpty())
            {
                _providerSummaries = await _providerRepository.GetAllProviderSummaries();
            }

            Dictionary<string, IEnumerable<string>> validationFailures = new Dictionary<string, IEnumerable<string>>();

            using (Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob))
            {
                if (datasetStream.Length == 0)
                {
                    _logger.Error($"Blob {blob.Name} contains no data");
                    validationFailures.Add(nameof(GetDatasetBlobModel.Filename), new string[] { $"Blob {blob.Name} contains no data" });
                }
                else
                {
                    using (ExcelPackage excelPackage = new ExcelPackage(datasetStream))
                    {
                        DatasetUploadValidationModel uploadModel = new DatasetUploadValidationModel(excelPackage, () => _providerSummaries, datasetDefinition);
                        ValidationResult validationResult = _datasetUploadValidator.Validate(uploadModel);
                        if (uploadModel.Data != null)
                        {
                            rowCount = uploadModel.Data.TableLoadResult.Rows.Count;
                        }
                        if (!validationResult.IsValid)
                        {
                            excelPackage.Save();

                            if (excelPackage.Stream.CanSeek)
                            {
                                excelPackage.Stream.Position = 0;
                            }

                            await blob.UploadFromStreamAsync(excelPackage.Stream);

                            string blobUrl = _blobClient.GetBlobSasUrl(blob.Name, DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);

                            validationFailures.Add("excel-validation-error", new string[] { string.Empty });
                            validationFailures.Add("error-message", new string[] { "The data source file does not match the schema rules" });
                            validationFailures.Add("blobUrl", new string[] { blobUrl });
                        }
                    }
                }
            }

            return (validationFailures, rowCount);
        }

        async Task<BuildProject> ProcessDataset(Dataset dataset, string specificationId, string relationshipId, int version, Reference user)
        {
            string dataDefinitionId = dataset.Definition.Id;

            DatasetVersion datasetVersion = dataset.History.Where(v => v.Version == version).SingleOrDefault();
            if (datasetVersion == null)
            {
                _logger.Error("Dataset version not found for dataset '{name}' ({id}) version '{version}'", dataset.Id, dataset.Name, version);
                throw new InvalidOperationException($"Dataset version not found for dataset '{dataset.Name}' ({dataset.Name}) version '{version}'");
            }

            string fullBlobName = datasetVersion.BlobName;

            DatasetDefinition datasetDefinition =
                    (await _datasetRepository.GetDatasetDefinitionsByQuery(m => m.Id == dataDefinitionId))?.FirstOrDefault();

            if (datasetDefinition == null)
            {
                _logger.Error($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");

                throw new Exception($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");
            }

            BuildProject buildProject = await _calcsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error($"Unable to find a build project for specification id: {specificationId}");

                throw new Exception($"Unable to find a build project for id: {specificationId}");
            }

            TableLoadResult loadResult = await GetTableResult(fullBlobName, datasetDefinition);

            if (loadResult == null)
            {
                _logger.Error($"Failed to load table result");

                throw new Exception($"Failed to load table result");
            }

            await PersistDataset(loadResult, dataset, datasetDefinition, buildProject, specificationId, relationshipId, version, user);

            return buildProject;
        }

        async Task PersistDataset(TableLoadResult loadResult, Dataset dataset, DatasetDefinition datasetDefinition, BuildProject buildProject, string specificationId, string relationshipId, int version, Reference user)
        {
            if (_providerSummaries.IsNullOrEmpty())
            {
                _providerSummaries = await _providerRepository.GetAllProviderSummaries();
            }

            Guard.IsNullOrWhiteSpace(relationshipId, nameof(relationshipId));

            IList<ProviderSourceDataset> providerSourceDatasets = new List<ProviderSourceDataset>();

            if (buildProject.DatasetRelationships == null)
            {
                _logger.Error($"No dataset relationships found for build project with id : '{buildProject.Id}' for specification '{specificationId}'");
                return;
            }

            DatasetRelationshipSummary relationshipSummary = buildProject.DatasetRelationships.FirstOrDefault(m => m.Relationship.Id == relationshipId);

            if (relationshipSummary == null)
            {
                _logger.Error($"No dataset relationship found for build project with id : {buildProject.Id} with data definition id {datasetDefinition.Id} and relationshipId '{relationshipId}'");
                return;
            }

            Dictionary<string, ProviderSourceDataset> existingCurrent = new Dictionary<string, ProviderSourceDataset>();

            IEnumerable<ProviderSourceDataset> existingCurrentDatasets = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.GetCurrentProviderSourceDatasets(specificationId, relationshipId));

            if (existingCurrentDatasets.AnyWithNullCheck())
            {
                foreach (ProviderSourceDataset currentDataset in existingCurrentDatasets)
                {
                    existingCurrent.Add(currentDataset.ProviderId, currentDataset);
                }
            }

            ConcurrentDictionary<string, ProviderSourceDataset> resultsByProviderId = new ConcurrentDictionary<string, ProviderSourceDataset>();

            ConcurrentDictionary<string, ProviderSourceDataset> updateCurrentDatasets = new ConcurrentDictionary<string, ProviderSourceDataset>();

            Parallel.ForEach(loadResult.Rows, (RowLoadResult row) =>
            {
                IEnumerable<string> allProviderIds = GetProviderIdsForIdentifier(datasetDefinition, row);

                foreach (string providerId in allProviderIds)
                {
                    if (!resultsByProviderId.TryGetValue(providerId, out ProviderSourceDataset sourceDataset))
                    {
                        sourceDataset = new ProviderSourceDataset
                        {
                            DataGranularity = relationshipSummary.DataGranularity,
                            SpecificationId = specificationId,
                            DefinesScope = relationshipSummary.DefinesScope,
                            DataDefinition = new Reference(relationshipSummary.DatasetDefinition.Id, relationshipSummary.DatasetDefinition.Name),
                            DataRelationship = new Reference(relationshipSummary.Relationship.Id, relationshipSummary.Relationship.Name),
                            DatasetRelationshipSummary = new Reference(relationshipSummary.Id, relationshipSummary.Name),
                            ProviderId = providerId,
                        };

                        sourceDataset.Current = new ProviderSourceDatasetVersion
                        {
                            Rows = new List<Dictionary<string, object>>(),
                            Dataset = new VersionReference(dataset.Id, dataset.Name, version),
                            Date = DateTimeOffset.Now.ToLocalTime(),
                            ProviderId = providerId,
                            Version = 1,
                            PublishStatus = PublishStatus.Draft,
                            ProviderSourceDatasetId = sourceDataset.Id,
                            Author = user
                        };

                        if (!resultsByProviderId.TryAdd(providerId, sourceDataset))
                        {
                            resultsByProviderId.TryGetValue(providerId, out sourceDataset);
                        }
                    }
                    sourceDataset.Current.Rows.Add(row.Fields);
                }
            });

            ConcurrentBag<ProviderSourceDatasetVersion> historyToSave = new ConcurrentBag<ProviderSourceDatasetVersion>();

            Parallel.ForEach(resultsByProviderId, (KeyValuePair<string, ProviderSourceDataset> providerSourceDataset) =>
            {
                string providerId = providerSourceDataset.Key;
                ProviderSourceDataset sourceDataset = providerSourceDataset.Value;

                ProviderSourceDatasetVersion newVersion = null;

                if (existingCurrent.ContainsKey(providerId))
                {
                    newVersion = existingCurrent[providerId].Current.Clone() as ProviderSourceDatasetVersion;
                    
                    string existingDatasetJson = JsonConvert.SerializeObject(existingCurrent[providerId].Current.Rows);
                    string latestDatasetJson = JsonConvert.SerializeObject(sourceDataset.Current.Rows);

                    if (existingDatasetJson != latestDatasetJson)
                    {
                        newVersion = _sourceDatasetsVersionRepository.CreateVersion(newVersion, existingCurrent[providerId].Current);
                        newVersion.Author = user;
                        newVersion.Rows = sourceDataset.Current.Rows;

                        sourceDataset.Current = newVersion;

                        updateCurrentDatasets.TryAdd(providerId, sourceDataset);

                        historyToSave.Add(newVersion);
                    }
                }
                else
                {
                    newVersion = sourceDataset.Current;

                    updateCurrentDatasets.TryAdd(providerId, sourceDataset);

                    historyToSave.Add(newVersion);
                }
            });

            if (updateCurrentDatasets.Count > 0)
            {
                _logger.Information($"Saving {updateCurrentDatasets.Count()} updated source datasets");

                await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.UpdateCurrentProviderSourceDatasets(updateCurrentDatasets.Values));
            }

            if (historyToSave.Any())
            {
                _logger.Information($"Saving {historyToSave.Count()} items to history");
                await _sourceDatasetsVersionRepository.SaveVersions(historyToSave);
            }

            await PopulateProviderSummariesForSpecification(specificationId, _providerSummaries);
        }

        async Task PopulateProviderSummariesForSpecification(string specificationId, IEnumerable<ProviderSummary> allCachedProviders)
        {
            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            IEnumerable<string> providerIdsAll = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.GetAllProviderIdsForSpecificationid(specificationId));

            IList<ProviderSummary> providerSummaries = new List<ProviderSummary>();

            foreach (string providerId in providerIdsAll)
            {
                ProviderSummary cachedProvider = allCachedProviders.FirstOrDefault(m => m.Id == providerId);

                if (cachedProvider != null)
                {
                    providerSummaries.Add(cachedProvider);
                }
            }

            await _cacheProvider.KeyDeleteAsync<ProviderSummary>(cacheKey);
            await _cacheProvider.CreateListAsync<ProviderSummary>(providerSummaries, cacheKey);
        }

        async Task<TableLoadResult> GetTableResult(string fullBlobName, DatasetDefinition datasetDefinition)
        {

            string dataset_cache_key = $"{CacheKeys.DatasetRows}:{datasetDefinition.Id}:{GetBlobNameCacheKey(fullBlobName)}".ToLowerInvariant();

            IEnumerable<TableLoadResult> tableLoadResults = await _cacheProvider.GetAsync<TableLoadResult[]>(dataset_cache_key);

            if (tableLoadResults.IsNullOrEmpty())
            {
                ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);

                if (blob == null)
                {
                    _logger.Error($"Failed to find blob with path: {fullBlobName}");
                    throw new ArgumentException($"Failed to find blob with path: {fullBlobName}");
                }

                using (Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob))
                {
                    if (datasetStream == null || datasetStream.Length == 0)
                    {
                        _logger.Error($"Invalid blob returned: {fullBlobName}");
                        throw new ArgumentException($"Invalid blob returned: {fullBlobName}");
                    }

                    tableLoadResults = _excelDatasetReader.Read(datasetStream, datasetDefinition).ToList();
                }

                await _cacheProvider.SetAsync(dataset_cache_key, tableLoadResults.ToArraySafe(), TimeSpan.FromDays(7), true);
            }

            return tableLoadResults.FirstOrDefault();
        }

        public async Task<IActionResult> GetCurrentDatasetVersionByDatasetId(HttpRequest request)
        {
            request.Query.TryGetValue("datasetId", out var datasetIdParse);

            string datasetId = datasetIdParse.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(datasetId))
            {
                _logger.Warning($"No {nameof(datasetId)} was provided to {nameof(GetCurrentDatasetVersionByDatasetId)}");

                return new BadRequestObjectResult($"Null or empty {nameof(datasetId)} provided");
            }

            DocumentEntity<Dataset> dataset = await _datasetRepository.GetDatasetDocumentByDatasetId(datasetId);
            if (dataset == null)
            {
                return new NotFoundObjectResult($"Unable to find dataset with ID: {datasetId}");
            }

            if (dataset.Content == null)
            {
                return new NotFoundObjectResult($"Unable to find dataset with ID: {datasetId}. Content is null");
            }

            if (dataset.Content.Current == null)
            {
                return new NotFoundObjectResult($"Unable to find dataset with ID: {datasetId}. Current version is null");
            }

            DatasetVersionResponseViewModel result = new DatasetVersionResponseViewModel()
            {
                Id = dataset.Id,
                Author = dataset.Content.Current.Author,
                BlobName = dataset.Content.Current.BlobName,
                Definition = dataset.Content.Definition,
                Description = dataset.Content.Description,
                LastUpdatedDate = dataset.UpdatedAt,
                Name = dataset.Content.Name,
                PublishStatus = dataset.Content.Current.PublishStatus,
                Version = dataset.Content.Current.Version,
                Comment = dataset.Content.Current.Commment,
                CurrentDataSourceRows = dataset.Content.Current.RowCount,
            };

            int maxVersion = dataset.Content.History.Max(m => m.Version);
            if (maxVersion > 1)
            {
                result.PreviousDataSourceRows = dataset.Content.History.First().RowCount;
            }

            return new OkObjectResult(result);
        }

        public static string GetBlobNameCacheKey(string blobPath)
        {
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(blobPath.ToLowerInvariant());
            return Convert.ToBase64String(plainTextBytes);
        }

        IDictionary<string, string> CreateMessageProperties(HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>();
            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }
    }
}
