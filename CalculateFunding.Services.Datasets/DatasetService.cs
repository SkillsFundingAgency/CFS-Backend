using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Providers.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Primitives;
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
        private readonly ISearchRepository<DatasetIndex> _datasetIndexSearchRepository;
        private readonly ISearchRepository<DatasetVersionIndex> _datasetVersionIndexRepository;
        private readonly IValidator<GetDatasetBlobModel> _getDatasetBlobModelValidator;
        private readonly ICacheProvider _cacheProvider;
        private readonly IProviderService _providerService;
        private readonly IValidator<ExcelPackage> _dataWorksheetValidator;
        private readonly IValidator<DatasetUploadValidationModel> _datasetUploadValidator;
        private readonly Policy _jobsApiClientPolicy;
        private readonly IJobsApiClient _jobsApiClient;

        static IEnumerable<ProviderSummary> _providerSummaries = new List<ProviderSummary>();

        public DatasetService(IBlobClient blobClient,
            ILogger logger,
            IDatasetRepository datasetRepository,
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator,
            IValidator<DatasetVersionUpdateModel> datasetVersionUpdateModelValidator,
            IMapper mapper,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator,
            ISearchRepository<DatasetIndex> datasetIndexSearchRepository,
            IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator,
            ICacheProvider cacheProvider,
            IProviderService providerService,
            IValidator<ExcelPackage> dataWorksheetValidator,
            IValidator<DatasetUploadValidationModel> datasetUploadValidator,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IJobsApiClient jobsApiClient,
            ISearchRepository<DatasetVersionIndex> datasetVersionIndexRepository)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(createNewDatasetModelValidator, nameof(createNewDatasetModelValidator));
            Guard.ArgumentNotNull(datasetVersionUpdateModelValidator, nameof(datasetVersionUpdateModelValidator));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(datasetMetadataModelValidator, nameof(datasetMetadataModelValidator));
            Guard.ArgumentNotNull(datasetIndexSearchRepository, nameof(datasetIndexSearchRepository));
            Guard.ArgumentNotNull(getDatasetBlobModelValidator, nameof(getDatasetBlobModelValidator));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(dataWorksheetValidator, nameof(dataWorksheetValidator));
            Guard.ArgumentNotNull(datasetUploadValidator, nameof(datasetUploadValidator));
            Guard.ArgumentNotNull(datasetsResiliencePolicies, nameof(datasetsResiliencePolicies));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));

            _blobClient = blobClient;
            _logger = logger;
            _datasetRepository = datasetRepository;
            _createNewDatasetModelValidator = createNewDatasetModelValidator;
            _datasetVersionUpdateModelValidator = datasetVersionUpdateModelValidator;
            _mapper = mapper;
            _datasetMetadataModelValidator = datasetMetadataModelValidator;
            _datasetIndexSearchRepository = datasetIndexSearchRepository;
            _getDatasetBlobModelValidator = getDatasetBlobModelValidator;
            _cacheProvider = cacheProvider;
            _providerService = providerService;
            _dataWorksheetValidator = dataWorksheetValidator;
            _datasetUploadValidator = datasetUploadValidator;
            _jobsApiClient = jobsApiClient;
            _datasetVersionIndexRepository = datasetVersionIndexRepository;
            _jobsApiClientPolicy = datasetsResiliencePolicies.JobsApiClient;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_datasetRepository).IsHealthOk();
            (bool Ok, string Message) searchRepoHealth = await _datasetIndexSearchRepository.IsHealthOk();
            (bool Ok, string Message) searchIndexVersionHealth = await _datasetVersionIndexRepository.IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(DatasetService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message });
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _datasetIndexSearchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchIndexVersionHealth.Ok, DependencyName = _datasetVersionIndexRepository.GetType().GetFriendlyName(), Message = searchIndexVersionHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }

        public async Task<IActionResult> CreateNewDataset(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CreateNewDatasetModel model = JsonConvert.DeserializeObject<CreateNewDatasetModel>(json);

            if (model == null)
            {
                _logger.Error("Null model name was provided to CreateNewDataset");
                return new BadRequestObjectResult("Null model name was provided");
            }
            BadRequestObjectResult validationResult = (await _createNewDatasetModelValidator.ValidateAsync(model)).PopulateModelState();

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
            BadRequestObjectResult validationResult = (await _datasetVersionUpdateModelValidator.ValidateAsync(model)).PopulateModelState();

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

        public async Task<IActionResult> GetDatasetByName(HttpRequest request)
        {
            request.Query.TryGetValue("datasetName", out Microsoft.Extensions.Primitives.StringValues dsName);

            string datasetName = dsName.FirstOrDefault();

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

        public async Task<IActionResult> GetDatasetsByDefinitionId(HttpRequest request)
        {
            request.Query.TryGetValue("definitionId", out Microsoft.Extensions.Primitives.StringValues definitionId);

            string datasetDefinitionId = definitionId.FirstOrDefault();

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
            request.Query.TryGetValue("operationId", out Microsoft.Extensions.Primitives.StringValues operationIdParse);

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

            Reference user = new Reference();

            if (blob.Metadata.ContainsKey("authorId") && blob.Metadata.ContainsKey("authorName"))
            {
                user.Id = blob.Metadata["authorId"];
                user.Name = blob.Metadata["authorName"];
            }
            else
            {
                user = request.GetUserOrDefault();
            }

            Trigger trigger = new Trigger
            {
                EntityId = model.DatasetId,
                EntityType = nameof(Dataset),
                Message = $"Validating dataset: '{model.DatasetId}' against definition: '{datasetDefinition.Name}'"
            };

            string correlationId = request.GetCorrelationId();

            JobCreateModel job = new JobCreateModel
            {
                InvokerUserDisplayName = user.Name,
                InvokerUserId = user.Id,
                JobDefinitionId = JobConstants.DefinitionNames.ValidateDatasetJob,
                MessageBody = JsonConvert.SerializeObject(model),
                Properties = new Dictionary<string, string>
                {
                    { "operation-id", responseModel.OperationId }
                },
                Trigger = trigger,
                CorrelationId = correlationId
            };

            await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(job));

            await _cacheProvider.SetAsync($"{CacheKeys.DatasetValidationStatus}:{responseModel.OperationId}", responseModel);

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

            string jobId = message.UserProperties["jobId"].ToString();

            await UpdateJobStatus(jobId, null, 0);

            GetDatasetBlobModel model = message.GetPayloadAsInstanceOf<GetDatasetBlobModel>();
            if (model == null)
            {
                _logger.Error("Null model was provided to ValidateDataset");
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, "Null model was provided to ValidateDataset");
                await UpdateJobStatus(jobId, false, 100, "Failed Validation - Null model was provided to ValidateDataset");

                return;
            }

            ValidationResult validationResult = (await _getDatasetBlobModelValidator.ValidateAsync(model));
            if (validationResult == null)
            {
                _logger.Error($"{nameof(GetDatasetBlobModel)} validation result returned null");
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, $"{nameof(GetDatasetBlobModel)} validation result returned null");
                await UpdateJobStatus(jobId, false, 100, "Failed Validation - no validation result");

                return;
            }
            else if (!validationResult.IsValid || validationResult.Errors.Count > 0)
            {
                _logger.Error($"{nameof(GetDatasetBlobModel)} model error: {{0}}", validationResult.Errors);
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, $"{nameof(GetDatasetBlobModel)} model error", ConvertToErrorDictionary(validationResult));
                await UpdateJobStatus(jobId, false, 100, "Failed Validation - model errors");

                return;
            }

            string fullBlobName = model.ToString();

            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);
            if (blob == null)
            {
                _logger.Error($"Failed to find blob with path: {fullBlobName}");
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, $"Failed to find blob with path: {fullBlobName}");
                await UpdateJobStatus(jobId, false, 100, "Failed Validation - file not found in blob storage");
                return;
            }

            await blob.FetchAttributesAsync();

            using (Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob))
            {
                if (datasetStream == null || datasetStream.Length == 0)
                {
                    _logger.Error($"Blob {blob.Name} contains no data");
                    await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, $"Blob {blob.Name} contains no data");
                    await UpdateJobStatus(jobId, false, 100, "Failed validation - file contains no data");

                    return;
                }

                try
                {
                    await SetValidationStatus(operationId, DatasetValidationStatus.ValidatingExcelWorkbook);
                    await UpdateJobStatus(jobId, null, 25);

                    using (ExcelPackage excel = new ExcelPackage(datasetStream))
                    {
                        validationResult = _dataWorksheetValidator.Validate(excel);

                        if (validationResult != null && (!validationResult.IsValid || validationResult.Errors.Count > 0))
                        {
                            await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, null, ConvertToErrorDictionary(validationResult));
                            await UpdateJobStatus(jobId, false, 100, "Failed validation - with validation errors");
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
                    await UpdateJobStatus(jobId, false, 100, "Failed validation - the data source file type is invalid");

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
                    await UpdateJobStatus(jobId, false, 100, "Failed Validation - invalid data definition");

                    return;
                }

                await SetValidationStatus(operationId, DatasetValidationStatus.ValidatingTableResults);
                await UpdateJobStatus(jobId, null, 50);

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
                        await UpdateJobStatus(jobId, null, 75);

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
                        await UpdateJobStatus(jobId, true, 100, "Dataset passed validation");
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
                    await UpdateJobStatus(jobId, false, 100, "Failed validation - table validation");
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

        private async Task UpdateJobStatus(string jobId, bool? completedSuccessfully, int percentComplete, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = completedSuccessfully,
                ItemsProcessed = percentComplete,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }

        public async Task<IActionResult> DownloadDatasetFile(HttpRequest request)
        {
            request.Query.TryGetValue("datasetId", out StringValues datasetId);
            request.Query.TryGetValue("datasetVersion", out StringValues datasetVersionStringValue);

            string currentDatasetId = datasetId.FirstOrDefault();
            string datasetVersionStr = null;

            if (datasetVersionStringValue.Count != 0)
            {
                datasetVersionStr = datasetVersionStringValue.First();
            }

            int datasetVersion = -1;

            if (!string.IsNullOrWhiteSpace(datasetVersionStr))
            {
                bool successfullyParsed = int.TryParse(datasetVersionStr, out datasetVersion);
                if (!successfullyParsed)
                {
                    return new BadRequestObjectResult("Invalid value was provided to datasetVersion");
                }
            }

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

            string fullBlobName = datasetVersion == -1 ? dataset.Current?.BlobName : dataset.History?.FirstOrDefault(dh => dh.Version == datasetVersion)?.BlobName;

            if (string.IsNullOrWhiteSpace(fullBlobName))
            {
                string errorLog = $"A blob name could not be found for dataset id: {currentDatasetId}";
                if (datasetVersion != -1)
                {
                    errorLog = $"{errorLog} version: {datasetVersion}";
                }

                _logger.Error(errorLog);

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
                    Status = Enum.GetName(typeof(Models.Versioning.PublishStatus), dataset.Content.Current.PublishStatus),
                    Description = dataset.Content.Description,
                    Version = dataset.Content.Current.Version,
                    ChangeNote = dataset.Content.Current.Comment,
                    LastUpdatedByName = dataset.Content.Current.Author?.Name,
                    LastUpdatedById = dataset.Content.Current.Author?.Id
                };

                searchEntries.Add(datasetIndex);

                if (searchEntries.Count >= searchBatchSize)
                {
                    foreach (IEnumerable<DatasetIndex> datasetVersionIndexBatch in searchEntries.ToBatches(searchBatchSize))
                    {
                        await _datasetIndexSearchRepository.Index(datasetVersionIndexBatch);
                        totalInserts += searchEntries.Count;
                    }
                    searchEntries.Clear();
                }
            }

            if (searchEntries.Any())
            {
                totalInserts += searchEntries.Count;
                await _datasetIndexSearchRepository.Index(searchEntries);
            }

            return new OkObjectResult($"Indexed total of {totalInserts} Datasets");
        }

        public async Task<IActionResult> ReindexDatasetVersions(HttpRequest request)
        {
            IEnumerable<DocumentEntity<Dataset>> datasets = await _datasetRepository.GetDatasets();
            int searchBatchSize = 100;

            int totalInserts = 0;

            List<DatasetVersionIndex> searchEntries = new List<DatasetVersionIndex>(searchBatchSize);

            foreach (DocumentEntity<Dataset> dataset in datasets)
            {
                foreach (DatasetVersion datasetVersion in dataset.Content.History)
                {
                    DatasetVersionIndex datasetVersionIndex = new DatasetVersionIndex()
                    {
                        Id = $"{dataset.Id}-{datasetVersion.Version}",
                        DatasetId = dataset.Id,
                        Name = dataset.Content.Name,
                        Version = datasetVersion.Version,
                        BlobName = datasetVersion.BlobName,
                        ChangeNote = datasetVersion.Comment,
                        DefinitionName = dataset.Content.Definition.Name,
                        Description = dataset.Content.Description,
                        LastUpdatedDate = datasetVersion.Date,
                        LastUpdatedByName = datasetVersion.Author.Name
                    };
                    searchEntries.Add(datasetVersionIndex);

                    if (searchEntries.Count >= searchBatchSize)
                    {
                        foreach (IEnumerable<DatasetVersionIndex> datasetVersionIndexBatch in searchEntries.ToBatches(searchBatchSize))
                        {
                            await _datasetVersionIndexRepository.Index(datasetVersionIndexBatch);
                            totalInserts += searchEntries.Count;
                        }
                        searchEntries.Clear();
                    }
                }
            }

            if (searchEntries.Any())
            {
                await _datasetVersionIndexRepository.Index(searchEntries);
                totalInserts += searchEntries.Count;
            }

            return new OkObjectResult($"Indexed total of {totalInserts} Dataset versions");
        }

        public async Task<IActionResult> RegenerateProviderSourceDatasets(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out Microsoft.Extensions.Primitives.StringValues specificationIdValues);

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
                if (relationship == null || relationship.DatasetVersion == null || string.IsNullOrWhiteSpace(relationship.DatasetVersion.Id))
                {
                    continue;
                }

                if (!datasets.TryGetValue(relationship.DatasetVersion.Id, out Dataset dataset))
                {
                    dataset = (await _datasetRepository.GetDatasetsByQuery(c => c.Id == relationship.DatasetVersion.Id)).FirstOrDefault();
                    datasets.Add(relationship.DatasetVersion.Id, dataset);
                }

                Reference user = request.GetUser();

                Trigger trigger = new Trigger
                {
                    EntityId = dataset.Id,
                    EntityType = nameof(Dataset),
                    Message = $"Mapping dataset: '{dataset.Id}'"
                };

                string correlationId = request.GetCorrelationId();

                JobCreateModel job = new JobCreateModel
                {
                    InvokerUserDisplayName = user.Name,
                    InvokerUserId = user.Id,
                    JobDefinitionId = JobConstants.DefinitionNames.MapDatasetJob,
                    MessageBody = JsonConvert.SerializeObject(dataset),
                    Properties = new Dictionary<string, string>
                    {
                        { "specification-id", relationship.Specification.Id },
                        { "relationship-id", relationship.Id }
                    },
                    SpecificationId = relationship.Specification.Id,
                    Trigger = trigger,
                    CorrelationId = correlationId
                };

                await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(job));
            }

            return new OkObjectResult(relationships);
        }

        public async Task UpdateDatasetAndVersionDefinitionName(Reference datsetDefinitionReference)
        {
            if (datsetDefinitionReference == null)
            {
                _logger.Error("Null dataset definition reference supplied");
                throw new NonRetriableException("A null dataset definition reference was supplied");
            }

            IEnumerable<Dataset> datasets = (await _datasetRepository.GetDatasetsByQuery(m => m.Definition.Id == datsetDefinitionReference.Id)).ToList();

            if (datasets.IsNullOrEmpty())
            {
                _logger.Information($"No datasets found to update for definition id: {datsetDefinitionReference.Id}");

                return;
            }

            foreach(Dataset dataset in datasets)
            {
                dataset.Definition.Name = datsetDefinitionReference.Name;
            }

            try
            {
                await _datasetRepository.SaveDatasets(datasets);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to save datasets to cosmos for definition id: {datsetDefinitionReference.Id}");

                throw new RetriableException($"Failed to save datasets to cosmos for definition id: {datsetDefinitionReference.Id}", ex);
            }

            List<IndexError> indexErrors = new List<IndexError>();

            foreach (Dataset dataset in datasets)
            {
                indexErrors.AddRange((await IndexDatasetInSearch(dataset)));

                foreach(DatasetVersion datasetVersion in dataset.History)
                {
                    indexErrors.AddRange(await IndexDatasetVersionInSearch(dataset, datasetVersion));
                }
            }

            if (indexErrors.Any())
            {
                string errors = string.Join(";", indexErrors.Select(m => m.ErrorMessage).ToArraySafe());

                _logger.Error($"Failed to save dataset to search for definition id: {datsetDefinitionReference.Id} in search with errors: {errors}");

                throw new RetriableException($"Failed to save dataset to search for definition id: {datsetDefinitionReference.Id} in search with errors: {errors}");
            }
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
            metadataModel.Comment = metadata.ContainsKey("comment") ? metadata["comment"] : string.Empty;

            ValidationResult validationResult = await _datasetMetadataModelValidator.ValidateAsync(metadataModel);

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
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                BlobName = blob.Name,
                RowCount = rowCount,
                Comment = metadataModel.Comment
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

            List<IndexError> indexErrors = (await IndexDatasetInSearch(dataset)).ToList();
            indexErrors.AddRange(await IndexDatasetVersionInSearch(dataset, newVersion));

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
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                BlobName = blob.Name,
                Comment = model.Comment,
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

            List<IndexError> indexErrors = (await IndexDatasetInSearch(dataset)).ToList();
            indexErrors.AddRange(await IndexDatasetVersionInSearch(dataset, newVersion));

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

            return _datasetIndexSearchRepository.Index(new List<DatasetIndex>
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
                    ChangeNote = dataset.Current.Comment,
                    LastUpdatedById = dataset.Current.Author?.Id,
                    LastUpdatedByName = dataset.Current.Author?.Name
                }
            });
        }

        private Task<IEnumerable<IndexError>> IndexDatasetVersionInSearch(Dataset dataset, DatasetVersion datasetVersion)
        {
            Guard.ArgumentNotNull(dataset, nameof(dataset));

            return _datasetVersionIndexRepository.Index(new List<DatasetVersionIndex>
            {
                new DatasetVersionIndex()
                {
                    Id = $"{dataset.Id}-{datasetVersion.Version}",
                    DatasetId = dataset.Id,
                    Name = dataset.Name,
                    Version = datasetVersion.Version,
                    BlobName = datasetVersion.BlobName,
                    ChangeNote = datasetVersion.Comment,
                    DefinitionName = dataset.Definition.Name,
                    Description = dataset.Description,
                    LastUpdatedDate = datasetVersion.Date,
                    LastUpdatedByName = datasetVersion.Author.Name
                }
            });
        }

        private async Task<(IDictionary<string, IEnumerable<string>> validationFailures, int providersProcessed)> ValidateTableResults(DatasetDefinition datasetDefinition, ICloudBlob blob)
        {
            int rowCount = 0;
            if (_providerSummaries.IsNullOrEmpty())
            {
                _providerSummaries = await _providerService.FetchCoreProviderData();
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
            request.Query.TryGetValue("datasetId", out Microsoft.Extensions.Primitives.StringValues datasetIdParse);

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
                Comment = dataset.Content.Current.Comment,
                CurrentDataSourceRows = dataset.Content.Current.RowCount,
            };

            int maxVersion = dataset.Content.History.Max(m => m.Version);
            if (maxVersion > 1)
            {
                result.PreviousDataSourceRows = dataset.Content.History.First().RowCount;
            }

            return new OkObjectResult(result);
        }
    }
}
