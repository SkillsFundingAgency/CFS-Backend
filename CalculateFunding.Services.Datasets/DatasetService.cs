using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using CalculateFunding.Models;
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
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
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
        private readonly ICacheProvider _cacheProvider;
        private readonly IProviderRepository _providerRepository;
        private readonly IValidator<ExcelPackage> _dataWorksheetValidator;
        private readonly IValidator<DatasetUploadValidationModel> _datasetUploadValidator;

        static IEnumerable<ProviderSummary> _providerSummaries = new List<ProviderSummary>();

        public DatasetService(IBlobClient blobClient,
            ILogger logger,
            IDatasetRepository datasetRepository,
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator,
            IValidator<DatasetVersionUpdateModel> datasetVersionUpdateModelValidator,
            IMapper mapper,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator,
            ISearchRepository<DatasetIndex> searchRepository,
            IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator,
            IMessengerService messengerService,
            ICacheProvider cacheProvider,
            IProviderRepository providerRepository,
            IValidator<ExcelPackage> dataWorksheetValidator,
            IValidator<DatasetUploadValidationModel> datasetUploadValidator)
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
            _cacheProvider = cacheProvider;
            _providerRepository = providerRepository;
            _dataWorksheetValidator = dataWorksheetValidator;
            _datasetUploadValidator = datasetUploadValidator;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var blobHealth = await _blobClient.IsHealthOk();
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_datasetRepository).IsHealthOk();
            var searchRepoHealth = await _searchRepository.IsHealthOk();
            string queueName = ServiceBusConstants.QueueNames.CalculationJobInitialiser;
            var messengerServiceHealth = await _messengerService.IsHealthOk(queueName);
            var cacheHealth = await _cacheProvider.IsHealthOk();
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



        private async Task<Dataset> SaveNewDatasetAndVersion(ICloudBlob blob, DatasetDefinition datasetDefinition, int rowCount)
        {
            Guard.ArgumentNotNull(blob, nameof(blob));
            Guard.ArgumentNotNull(datasetDefinition, nameof(datasetDefinition));
            Guard.ArgumentNotNull(rowCount, nameof(rowCount));

            IDictionary<string, string> metadata = blob.Metadata;

            Guard.ArgumentNotNull(metadata, nameof(metadata));

            DatasetMetadataModel metadataModel = new DatasetMetadataModel();

            metadataModel.AuthorName = metadata.ContainsKey("authorName") ? metadata["authorName"] : string.Empty;
            metadataModel.AuthorId = metadata.ContainsKey("authorId") ? metadata["authorId"] : string.Empty;
            metadataModel.DatasetId = metadata.ContainsKey("datasetId") ? metadata["datasetId"] : string.Empty;
            metadataModel.DataDefinitionId = metadata.ContainsKey("dataDefinitionId") ? metadata["dataDefinitionId"] : string.Empty;
            metadataModel.Name = metadata.ContainsKey("name") ? metadata["name"] : string.Empty;
            metadataModel.Description = metadata.ContainsKey("description") ? HttpUtility.UrlDecode(metadata["description"]) : string.Empty;

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
