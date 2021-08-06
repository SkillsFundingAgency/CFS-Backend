using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using OfficeOpenXml;
using Polly;
using Serilog;
using ApiClientProviders = CalculateFunding.Common.ApiClient.Providers;
using JobCreateModel = CalculateFunding.Common.ApiClient.Jobs.Models.JobCreateModel;
using Trigger = CalculateFunding.Common.ApiClient.Jobs.Models.Trigger;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.DataImporter.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using Microsoft.Azure.Cosmos.Linq;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetService : JobProcessingService, IDatasetService, IHealthChecker
    {
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        private readonly IDatasetRepository _datasetRepository;
        private readonly IVersionRepository<DatasetVersion> _versionDatasetRepository;
        private readonly IValidator<CreateNewDatasetModel> _createNewDatasetModelValidator;
        private readonly IValidator<DatasetVersionUpdateModel> _datasetVersionUpdateModelValidator;
        private readonly IMapper _mapper;
        private readonly IValidator<DatasetMetadataModel> _datasetMetadataModelValidator;
        private readonly ISearchRepository<DatasetIndex> _datasetIndexSearchRepository;
        private readonly ISearchRepository<DatasetVersionIndex> _datasetVersionIndexRepository;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly IValidator<GetDatasetBlobModel> _getDatasetBlobModelValidator;
        private readonly ICacheProvider _cacheProvider;
        private readonly IValidator<ExcelPackage> _dataWorksheetValidator;
        private readonly IValidator<DatasetUploadValidationModel> _datasetUploadValidator;
        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly IJobManagement _jobManagement;
        private readonly IProviderSourceDatasetRepository _providerSourceDatasetRepository;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IPolicyRepository _policyRepository;
        private readonly IDatasetDataMergeService _datasetDataMergeService;

        public DatasetService(
            IBlobClient blobClient,
            ILogger logger,
            IDatasetRepository datasetRepository,
            IVersionRepository<DatasetVersion> versionDatasetRepository,
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator,
            IValidator<DatasetVersionUpdateModel> datasetVersionUpdateModelValidator,
            IMapper mapper,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator,
            ISearchRepository<DatasetIndex> datasetIndexSearchRepository,
            IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator,
            ICacheProvider cacheProvider,
            IValidator<ExcelPackage> dataWorksheetValidator,
            IValidator<DatasetUploadValidationModel> datasetUploadValidator,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            ISearchRepository<DatasetVersionIndex> datasetVersionIndexRepository,
            IProvidersApiClient providersApiClient,
            IJobManagement jobManagement,
            IProviderSourceDatasetRepository providerSourceDatasetRepository,
            ISpecificationsApiClient specificationsApiClient,
            IPolicyRepository policyRepository,
            IDatasetDataMergeService datasetDataMergeService) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(versionDatasetRepository, nameof(versionDatasetRepository));
            Guard.ArgumentNotNull(createNewDatasetModelValidator, nameof(createNewDatasetModelValidator));
            Guard.ArgumentNotNull(datasetVersionUpdateModelValidator, nameof(datasetVersionUpdateModelValidator));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(datasetMetadataModelValidator, nameof(datasetMetadataModelValidator));
            Guard.ArgumentNotNull(datasetIndexSearchRepository, nameof(datasetIndexSearchRepository));
            Guard.ArgumentNotNull(getDatasetBlobModelValidator, nameof(getDatasetBlobModelValidator));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(dataWorksheetValidator, nameof(dataWorksheetValidator));
            Guard.ArgumentNotNull(datasetUploadValidator, nameof(datasetUploadValidator));
            Guard.ArgumentNotNull(datasetsResiliencePolicies, nameof(datasetsResiliencePolicies));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.ProvidersApiClient, nameof(datasetsResiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(providerSourceDatasetRepository, nameof(providerSourceDatasetRepository));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(datasetDataMergeService, nameof(datasetDataMergeService));

            _blobClient = blobClient;
            _logger = logger;
            _datasetRepository = datasetRepository;
            _versionDatasetRepository = versionDatasetRepository;
            _createNewDatasetModelValidator = createNewDatasetModelValidator;
            _datasetVersionUpdateModelValidator = datasetVersionUpdateModelValidator;
            _mapper = mapper;
            _datasetMetadataModelValidator = datasetMetadataModelValidator;
            _datasetIndexSearchRepository = datasetIndexSearchRepository;
            _getDatasetBlobModelValidator = getDatasetBlobModelValidator;
            _cacheProvider = cacheProvider;
            _dataWorksheetValidator = dataWorksheetValidator;
            _datasetUploadValidator = datasetUploadValidator;
            _datasetVersionIndexRepository = datasetVersionIndexRepository;
            _providersApiClient = providersApiClient;
            _providersApiClientPolicy = datasetsResiliencePolicies.ProvidersApiClient;
            _jobManagement = jobManagement;
            _providerSourceDatasetRepository = providerSourceDatasetRepository;
            _specificationsApiClient = specificationsApiClient;
            _policyRepository = policyRepository;
            _datasetDataMergeService = datasetDataMergeService;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker) _datasetRepository).IsHealthOk();
            (bool Ok, string Message) searchRepoHealth = await _datasetIndexSearchRepository.IsHealthOk();
            (bool Ok, string Message) searchIndexVersionHealth = await _datasetVersionIndexRepository.IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(DatasetService)
            };
            health.Dependencies.Add(new DependencyHealth {HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message});
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth {HealthOk = searchRepoHealth.Ok, DependencyName = _datasetIndexSearchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message});
            health.Dependencies.Add(new DependencyHealth {HealthOk = searchIndexVersionHealth.Ok, DependencyName = _datasetVersionIndexRepository.GetType().GetFriendlyName(), Message = searchIndexVersionHealth.Message});
            health.Dependencies.Add(new DependencyHealth {HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message});

            return health;
        }

        public async Task<IActionResult> CreateAndPersistNewDataset(CreateNewDatasetModel model, Reference author)
        {
            if (model == null)
            {
                _logger.Error("Null model name was provided to CreateAndPersistNewDataset");
                return new BadRequestObjectResult("Null model name was provided");
            }

            model.StrictValidation = false;

            BadRequestObjectResult validationResult = (await _createNewDatasetModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            FundingStream fundingStream = await _policyRepository.GetFundingStream(model.FundingStreamId);

            string datasetId = Guid.NewGuid().ToString();
            string filePath = GetUploadedBlobFilepath(model.Filename, datasetId, 1);

            DatasetVersion newVersion = new DatasetVersion
            {
                DatasetId = datasetId,
                Author = new Reference(author.Id, author.Name),
                Version = 1,
                Date = DateTimeOffset.Now,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                BlobName = model.Filename,
                UploadedBlobFilePath = filePath,
                ChangeType = DatasetChangeType.NewVersion,
                RowCount = model.RowCount,
                FundingStream = fundingStream
            };

            Dataset dataset = new Dataset
            {
                Id = datasetId,
                Name = model.Name,
                Current = newVersion
            };

            HttpStatusCode versionStatusCode = await _versionDatasetRepository.SaveVersion(newVersion);
            if (!versionStatusCode.IsSuccess())
            {
                _logger.Error($"Failed to create dataset version for: {model.Name} with status code {versionStatusCode}");

                throw new InvalidOperationException($"Failed to create dataset version for: {model.Name} with status code {versionStatusCode}");
            }

            HttpStatusCode datasetStatusCode = await _datasetRepository.SaveDataset(dataset);
            if (!datasetStatusCode.IsSuccess())
            {
                _logger.Error($"Failed to create dataset for: {model.Name} with status code {datasetStatusCode}");

                throw new InvalidOperationException($"Failed to create dataset for id: {model.Name} with status code {datasetStatusCode}");
            }

            List<IndexError> indexErrors = (await IndexDatasetInSearch(dataset)).ToList();
            indexErrors.AddRange(await IndexDatasetVersionInSearch(dataset, newVersion));

            if (indexErrors.Any())
            {
                string errors = string.Join(";", indexErrors.Select(m => m.ErrorMessage).ToArraySafe());

                _logger.Error($"Failed to save dataset for: {model.Name} in search with errors {errors}");

                throw new InvalidOperationException($"Failed to save dataset for: {model.Name} in search with errors {errors}");
            }

            NewDatasetVersionResponseModel responseModel = _mapper.Map<NewDatasetVersionResponseModel>(model);

            responseModel.DatasetId = datasetId;
            responseModel.Filename = model.Filename;
            responseModel.Author = author;
            responseModel.DefinitionId = model.DefinitionId;
            responseModel.FundingStreamId = model.FundingStreamId;

            return new OkObjectResult(responseModel);
        }

        public async Task<IActionResult> CreateNewDataset(CreateNewDatasetModel model, Reference author)
        {
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

            string datasetId = Guid.NewGuid().ToString();

            string filePath = GetUploadedBlobFilepath(model.Filename, datasetId, 1);
            string blobUrl = _blobClient.GetBlobSasUrl(filePath,
                DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);

            NewDatasetVersionResponseModel responseModel = _mapper.Map<NewDatasetVersionResponseModel>(model);

            responseModel.DatasetId = datasetId;
            responseModel.BlobUrl = blobUrl;
            responseModel.Filename = model.Filename;
            responseModel.Author = author;
            responseModel.DefinitionId = model.DefinitionId;
            responseModel.FundingStreamId = model.FundingStreamId;

            return new OkObjectResult(responseModel);
        }

        public async Task<IActionResult> DatasetVersionUpdate(DatasetVersionUpdateModel model, Reference author)
        {
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

            int version = await _versionDatasetRepository.GetNextVersionNumber(dataset.Current);
            
            string blobUrl = _blobClient.GetBlobSasUrl(GetUploadedBlobFilepath(model.Filename, model.DatasetId, version),
                DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);

            NewDatasetVersionResponseModel responseModel = _mapper.Map<NewDatasetVersionResponseModel>(model);

            responseModel.DatasetId = dataset.Id;
            responseModel.BlobUrl = blobUrl;
            responseModel.Author = author;
            responseModel.DefinitionId = dataset.Definition.Id;
            responseModel.Name = dataset.Name;
            responseModel.Version = version;
            responseModel.FundingStreamId = model.FundingStreamId;

            return new OkObjectResult(responseModel);
        }

        public async Task<IActionResult> GetDatasetByDatasetId(string datasetId)
        {
            if (string.IsNullOrWhiteSpace(datasetId))
            {
                _logger.Error("No dataset id was provided to GetDatasetByDatasetId");

                return new BadRequestObjectResult("Null or empty dataset id provided");
            }

            Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(datasetId);

            if (dataset == null)
            {
                _logger.Information($"Dataset was not found for id: {datasetId}");

                return new NotFoundResult();
            }

            _logger.Information($"Dataset found for id: {datasetId}");

            return new OkObjectResult(_mapper.Map<DatasetViewModel>(dataset));
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

            IEnumerable<Dataset> datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Content.Name.ToLower() == datasetName.ToLower());

            if (!datasets.Any())
            {
                _logger.Information($"Dataset was not found for name: {datasetName}");

                return new NotFoundResult();
            }

            _logger.Information($"Dataset found for name: {datasetName}");

            return new OkObjectResult(datasets.FirstOrDefault());
        }

        public async Task<IActionResult> GetDatasetsByDefinitionId(string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                _logger.Error($"No {nameof(definitionId)} was provided to {nameof(GetDatasetsByDefinitionId)}");

                return new BadRequestObjectResult($"Null or empty {nameof(definitionId)} provided");
            }

            IEnumerable<Dataset> datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Content.Definition.Id == definitionId.ToLower());

            IEnumerable<DatasetViewModel> result = datasets?.Select(_mapper.Map<DatasetViewModel>).ToArraySafe();

            return new OkObjectResult(result ?? Enumerable.Empty<DatasetViewModel>());
        }

        public async Task<IActionResult> GetValidateDatasetStatus(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId))
            {
                _logger.Error($"No {nameof(operationId)} was provided to {nameof(GetValidateDatasetStatus)}");

                return new BadRequestObjectResult($"Null or empty {nameof(operationId)} provided");
            }

            DatasetValidationStatusModel status = await _cacheProvider.GetAsync<DatasetValidationStatusModel>($"{CacheKeys.DatasetValidationStatus}:{operationId}");
            if (status == null)
            {
                return new NotFoundObjectResult("Unable to find Dataset Validation Status");
            }

            return new OkObjectResult(status);
        }

        public async Task<IActionResult> ValidateDataset(GetDatasetBlobModel model, Reference author, string correlationId)
        {
            if (model == null)
            {
                _logger.Error("Null model name was provided to ValidateDataset");
                return new BadRequestObjectResult("Null model name was provided");
            }

            ValidationResult validationResult = await _getDatasetBlobModelValidator.ValidateAsync(model);

            if (validationResult != null && (!validationResult.IsValid || validationResult.Errors.Count > 0))
            {
                _logger.Error($"{nameof(GetDatasetBlobModel)} model error: {0}", validationResult.Errors);

                return validationResult.PopulateModelState();
            }

            string uploadedBlobFilepath = GetUploadedBlobFilepath(model.Filename, model.DatasetId, model.Version);
            bool exists = await _blobClient.BlobExistsAsync(uploadedBlobFilepath);
            if (!exists)
            {
                _logger.Error($"Blob '{uploadedBlobFilepath}' does not exist.");

                return new NotFoundResult();
            }
            
            ICloudBlob uploadedBlob = await _blobClient.GetBlobReferenceFromServerAsync(uploadedBlobFilepath);
            if (uploadedBlob == null)
            {
                _logger.Error($"Failed to find blob with path: {uploadedBlobFilepath}");
                return new PreconditionFailedResult($"Failed to find blob with path: {uploadedBlobFilepath}");
            }

            await uploadedBlob.FetchAttributesAsync();

            using (Stream datasetStream = await _blobClient.DownloadToStreamAsync(uploadedBlob))
            {
                if (datasetStream == null || datasetStream.Length == 0)
                {
                    _logger.Error($"Blob {uploadedBlob.Name} contains no data");
                    return new PreconditionFailedResult($"Blob {uploadedBlob.Name} contains no data");
                }
            }

            string dataDefinitionId = uploadedBlob.Metadata["dataDefinitionId"];

            DocumentEntity<DatasetDefinition> datasetDefinitionDocumentEntity =
                await _datasetRepository.GetDatasetDefinitionDocumentByDatasetDefinitionId(dataDefinitionId);

            if (datasetDefinitionDocumentEntity?.Content == null)
            {
                _logger.Error($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {uploadedBlobFilepath}");

                return new PreconditionFailedResult($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {uploadedBlobFilepath}");
            }

            if (model.MergeExistingVersion)
            {
                DocumentEntity<Dataset> datasetDocumentEntity = await _datasetRepository.GetDatasetDocumentByDatasetId(model.DatasetId);

                if (datasetDocumentEntity?.Content == null)
                {
                    _logger.Error($"Unable to find a dataset for id: {dataDefinitionId}, for blob: {uploadedBlobFilepath}");

                    return new PreconditionFailedResult($"Unable to find a dataset for id: {dataDefinitionId}, for blob: {uploadedBlobFilepath}");
                }

                if (datasetDefinitionDocumentEntity.Content.Version > datasetDocumentEntity.Content.Definition.Version)
                {
                    string errorMessage =
                        "There has been data schema change since the last version of this data source file was uploaded. Retry uploading with the create new version option";

                    _logger.Error(errorMessage);

                    string[] errors = new[] {errorMessage};

                    return new BadRequestObjectResult(errors.ToModelStateDictionary());
                }
            }

            DatasetValidationStatusModel responseModel = new DatasetValidationStatusModel
            {
                OperationId = Guid.NewGuid().ToString(),
            };

            if (uploadedBlob.Metadata.ContainsKey("datasetId"))
            {
                responseModel.DatasetId = uploadedBlob.Metadata["datasetId"];
                model.DatasetId = uploadedBlob.Metadata["datasetId"];
            }

            Reference user = new Reference();

            if (uploadedBlob.Metadata.ContainsKey("authorId") && uploadedBlob.Metadata.ContainsKey("authorName"))
            {
                user.Id = uploadedBlob.Metadata["authorId"];
                user.Name = uploadedBlob.Metadata["authorName"];
            }
            else
            {
                user = author;
            }

            model.LastUpdatedById = user?.Id;
            model.LastUpdatedByName = user?.Name;

            Trigger trigger = new Trigger
            {
                EntityId = model.DatasetId,
                EntityType = nameof(Dataset),
                Message = $"Validating dataset: '{model.DatasetId}' against definition: '{datasetDefinitionDocumentEntity.Content.Name}'"
            };

            JobCreateModel job = new JobCreateModel
            {
                InvokerUserDisplayName = user?.Name,
                InvokerUserId = user?.Id,
                JobDefinitionId = JobConstants.DefinitionNames.ValidateDatasetJob,
                MessageBody = JsonConvert.SerializeObject(model),
                Properties = new Dictionary<string, string>
                {
                    {"operation-id", responseModel.OperationId},
                    {"dataset-id", model.DatasetId }
                },
                Trigger = trigger,
                CorrelationId = correlationId
            };

            Job validateDatasetJob = await _jobManagement.QueueJob(job);

            await _cacheProvider.SetAsync($"{CacheKeys.DatasetValidationStatus}:{responseModel.OperationId}", responseModel);

            responseModel.ValidateDatasetJobId = validateDatasetJob.Id;

            return new OkObjectResult(responseModel);
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string operationId = message.GetOperationId();

            if (string.IsNullOrWhiteSpace(operationId))
            {
                _logger.Error($"Operation ID was null or empty string on the message from {nameof(ValidateDataset)}");

                return;
            }

            await SetValidationStatus(operationId, DatasetValidationStatus.Processing);

            GetDatasetBlobModel model = message.GetPayloadAsInstanceOf<GetDatasetBlobModel>();

            if (model == null)
            {
                string errorMessage = "Null model was provided to ValidateDataset";

                _logger.Error(errorMessage);
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage);
                throw new NonRetriableException($"Failed Validation - {errorMessage}");
            }

            ValidationResult validationResult = await _getDatasetBlobModelValidator.ValidateAsync(model);
            if (validationResult == null)
            {
                string errorMessage = $"{nameof(GetDatasetBlobModel)} validation result returned null";

                _logger.Error(errorMessage);
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage);
                throw new NonRetriableException("Failed Validation - no validation result");
            }
            else if (!validationResult.IsValid || validationResult.Errors.Count > 0)
            {
                string errorMessage = $"{nameof(GetDatasetBlobModel)} model error";

                _logger.Error($"{errorMessage}: {{0}}", validationResult.Errors);
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage, ConvertToErrorDictionary(validationResult));
                throw new NonRetriableException("Failed Validation - model errors");
            }

            string uploadedBlobPath = GetUploadedBlobFilepath(model.Filename, model.DatasetId, model.Version);
            string blobPath = GetMergedBlobFilepath(model.Filename, model.DatasetId, model.Version);
            ICloudBlob blob = await _blobClient.CopyBlobAsync(uploadedBlobPath, blobPath);

            if (blob == null)
            {
                string errorMessage = $"Failed to find blob with path: {uploadedBlobPath}";

                _logger.Error(errorMessage);
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage);
                throw new NonRetriableException("Failed Validation - file not found in blob storage");
            }

            await blob.FetchAttributesAsync();

            using Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob);
            if (datasetStream == null || datasetStream.Length == 0)
            {
                string errorMessage = $"Blob {blob.Name} contains no data";

                _logger.Error(errorMessage);
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage);
                throw new NonRetriableException("Failed validation - file contains no data");
            }

            Dataset dataset;
            IEnumerable<Dataset> datasets = _datasetRepository.GetDatasetsByQuery(m => m.Content.Name.ToLower() == blob.Metadata["name"].ToLower()).Result;
            if (datasets != null && datasets.Any() &&
                (datasets.Any(d => d.Id != model.DatasetId) || datasets.Any(d => d.Id == model.DatasetId && d.Current.Version >= model.Version)))
            {
                string errorMessage = $"Dataset {blob.Metadata["name"]} needs to be a unique name";

                _logger.Error(errorMessage);

                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage);
                throw new NonRetriableException("Failed validation - dataset name needs to be unique");
            }
            else
            {
                dataset = datasets.FirstOrDefault(d => d.Id == model.DatasetId);
            }

            string dataDefinitionId = blob.Metadata["dataDefinitionId"];
            DatasetDefinition datasetDefinition =
                (await _datasetRepository.GetDatasetDefinitionsByQuery(m => m.Id == dataDefinitionId)).FirstOrDefault();

            if (datasetDefinition == null)
            {
                string errorMessage = $"Unable to find a data definition for id: {dataDefinitionId}, for blob: {uploadedBlobPath}";
                _logger.Error(errorMessage);

                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage);
                throw new NonRetriableException("Failed Validation - invalid data definition");
            }

            string fundingStreamId = blob.Metadata["fundingStreamId"];
            IEnumerable<PoliciesApiModels.FundingStream> fundingStreams = await _policyRepository.GetFundingStreams();

            if (!fundingStreams.Select(_ => _.Id).Contains(fundingStreamId))
            {
                string errorMessage = $"Unable to validate given funding stream ID: {fundingStreamId}";
                _logger.Error(errorMessage);

                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, errorMessage);
                throw new NonRetriableException("Failed Validation - invalid funding stream ID");
            }

            PoliciesApiModels.FundingStream fundingStream = fundingStreams.SingleOrDefault(_ => _.Id == fundingStreamId);
            DatasetDataMergeResult mergeResult = new DatasetDataMergeResult();
            IDictionary<string, IEnumerable<string>> validationFailures = new Dictionary<string, IEnumerable<string>>();
            int validationRowCount = 0;
            try
            {
                await SetValidationStatus(operationId, DatasetValidationStatus.ValidatingExcelWorkbook);
                await NotifyPercentComplete(25);

                using ExcelPackage excel = new ExcelPackage(datasetStream);
                
                validationResult = _dataWorksheetValidator.Validate(excel);

                if (validationResult != null && (!validationResult.IsValid || validationResult.Errors.Count > 0))
                {
                    await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, null, ConvertToErrorDictionary(validationResult));
                    throw new NonRetriableException($"Failed validation - {GetValidationResultErrors(validationResult)}"); 
                }
                else if (model.MergeExistingVersion && dataset != null)
                {
                    (validationFailures, _) = await ValidateTableResults(datasetDefinition, datasetStream, model.EmptyFieldEvaluationOption, dataset.Current.BlobName);
                    if (validationFailures.Count > 0)
                    {
                        await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, null, validationFailures);
                        throw new NonRetriableException($"Failed validation - {GetValidationFailuresValues(validationFailures)}");
                    }

                    await NotifyPercentComplete(35);
                    await SetValidationStatus(operationId, DatasetValidationStatus.MergeInprogress);

                    mergeResult = await _datasetDataMergeService.Merge(datasetDefinition, 
                        dataset.Current.BlobName, 
                        blobPath, 
                        model.EmptyFieldEvaluationOption);

                    dataset.Current.BlobName = blobPath;
                    dataset.Current.UploadedBlobFilePath = uploadedBlobPath;
                    dataset.Current.ChangeType = model.MergeExistingVersion ? DatasetChangeType.Merge : DatasetChangeType.NewVersion;
                    dataset.Current.ProviderVersionId = null;

                    if (!mergeResult.HasChanges)
                    {
                        // no need to update index as we are just saving the merge results
                        dataset.Current.NewRowCount = mergeResult.TotalRowsCreated;
                        dataset.Current.AmendedRowCount = mergeResult.TotalRowsAmended;

                        HttpStatusCode statusCode = await _datasetRepository.SaveDataset(dataset);

                        if (!statusCode.IsSuccess())
                        {
                            _logger.Warning($"Failed to save dataset for id: {model.DatasetId} with status code {statusCode}");

                            throw new InvalidOperationException($"Failed to save dataset for id: {model.DatasetId} with status code {statusCode}");
                        }

                        await SetValidationStatus(operationId, DatasetValidationStatus.Validated, datasetId: dataset.Id);

                        // set the outcome of the job
                        Outcome = "Merge completed but no changes detected.";

                        return;
                    }

                    if (mergeResult.HasErrors)
                    {
                        await SetValidationStatus(operationId, DatasetValidationStatus.MergeFailed, mergeResult.ErrorMessage);
                        throw new NonRetriableException(mergeResult.ErrorMessage);
                    }
                    else
                    {
                        await SetValidationStatus(operationId, DatasetValidationStatus.MergeCompleted, mergeResult.GetMergeResultsMessage());
                    }
                }
            }
            catch (Exception exception)
            {
                if (exception is NonRetriableException)
                {
                    throw;
                }
                else
                {
                    const string errorMessage = "The data source file type is invalid. Check that your file is an xls or xlsx file";

                    _logger.Error(exception, errorMessage);

                    validationResult = new ValidationResult();
                    validationResult.Errors.Add(new ValidationFailure("typical-model-validation-error", string.Empty));
                    validationResult.Errors.Add(new ValidationFailure(nameof(model.Filename), errorMessage));

                    await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, null, ConvertToErrorDictionary(validationResult));
                    throw new NonRetriableException("Failed validation - the data source file type is invalid");
                }
            }

            await NotifyPercentComplete(50);
            await SetValidationStatus(operationId, DatasetValidationStatus.ValidatingTableResults);

            (validationFailures, validationRowCount) = await ValidateTableResults(datasetDefinition, datasetStream, model.EmptyFieldEvaluationOption, dataset?.Current?.BlobName);

            if (validationFailures.Count > 0)
            {
                await SetValidationStatus(operationId, DatasetValidationStatus.FailedValidation, null, validationFailures);
                throw new NonRetriableException($"Failed validation - {GetValidationFailuresValues(validationFailures)}");
            }

            await SaveDataset(message, operationId, datasetDefinition, blob, uploadedBlobPath, model, fundingStream, mergeResult, validationRowCount);
        }
        

        private static string GetValidationResultErrors(ValidationResult validationResult) =>
            string.Join(";", validationResult.Errors.Select(m => m.ErrorMessage).ToArraySafe());

        private static StringBuilder GetValidationFailuresValues(IDictionary<string, IEnumerable<string>> validationFailures)
        {
            StringBuilder errors = new StringBuilder();
            validationFailures.ForEach(validationFailure => validationFailure.Value.Where(failure => !string.IsNullOrEmpty(failure)).ForEach(e =>
            {
                errors.Append(e);
                errors.Append(";");
            }));
            return errors;
        }

        private async Task SaveDataset(Message message, 
            string operationId, 
            DatasetDefinition datasetDefinition,
            ICloudBlob blob, 
            string uploadedBlobFilePath,
            GetDatasetBlobModel model, 
            PoliciesApiModels.FundingStream fundingStream, 
            DatasetDataMergeResult mergeResult, int rowCount)
        {
            Dataset dataset;
            try
            {
                await SetValidationStatus(operationId, DatasetValidationStatus.SavingResults);
                await NotifyPercentComplete(75);

                if (model.Version == 1)
                {
                    dataset = await SaveNewDatasetAndVersion(blob, datasetDefinition, rowCount, uploadedBlobFilePath, fundingStream);
                }
                else
                {
                    Reference user = new Reference();

                    if (!string.IsNullOrWhiteSpace(model.LastUpdatedById) &&
                        !string.IsNullOrWhiteSpace(model.LastUpdatedByName))
                    {
                        user = new Reference(model.LastUpdatedById, model.LastUpdatedByName);
                    }
                    else
                    {
                        user = message.GetUserDetails();
                    }

                    dataset = await UpdateExistingDatasetAndAddVersion(blob, model, user, rowCount, uploadedBlobFilePath, fundingStream, mergeResult);
                }

                await SetValidationStatus(operationId, DatasetValidationStatus.Validated, datasetId: dataset.Id);

                // set the outcome of the job
                Outcome = "Dataset passed validation";
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to save the dataset or dataset version during validation");

                await SetValidationStatus(operationId, DatasetValidationStatus.ExceptionThrown, exception.Message);
                throw;
            }
        }

        private async Task FailValidation(Stream fileStream)
        {
            Job.CompletionStatus = CompletionStatus.Succeeded;
            Outcome = "ValidationFailed";

            string blobName = $"validation-errors/{Job.Id}.xlsx";

            ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName);

            await blob.UploadFromStreamAsync(fileStream);
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

        private async Task SetValidationStatus(
            string operationId,
            DatasetValidationStatus currentOperation,
            string errorMessage = null,
            IDictionary<string, IEnumerable<string>> validationFailures = null,
            string datasetId = null)
        {
            DatasetValidationStatusModel status = new DatasetValidationStatusModel
            {
                OperationId = operationId,
                CurrentOperation = currentOperation,
                ErrorMessage = errorMessage,
                ValidationFailures = validationFailures,
                LastUpdated = DateTimeOffset.Now,
                DatasetId = datasetId,
            };

            await _cacheProvider.SetAsync($"{CacheKeys.DatasetValidationStatus}:{status.OperationId}", status);
        }

        public async Task<IActionResult> UploadDatasetFile(string filename, DatasetMetadataViewModel model)
        {
            string uploadedBlobFilepath = GetUploadedBlobFilepath(filename, model.DatasetId, 1);
            ICloudBlob blob = _blobClient.GetBlockBlobReference(uploadedBlobFilepath);

            using (MemoryStream stream = new MemoryStream(model.Stream))
            {
                await blob.UploadFromStreamAsync(stream);
            }

            blob.Metadata["dataDefinitionId"] = model.DataDefinitionId;
            blob.Metadata["datasetId"] = model.DatasetId;
            blob.Metadata["authorId"] = model.AuthorId;
            blob.Metadata["authorName"] = model.AuthorName;
            blob.Metadata["name"] = model.Name;
            blob.Metadata["description"] = model.Description;
            blob.Metadata["fundingStreamId"] = model.FundingStreamId;
            blob.Metadata["converterWizard"] = model.ConverterEligible.ToString();
            blob.SetMetadata();

            return new OkResult();
        }

        public async Task<IActionResult> DownloadDatasetFile(string currentDatasetId, string datasetVersionStr)
        {
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
            
            string fullBlobName = datasetVersion == -1 ? dataset.Current?.BlobName : (await _versionDatasetRepository.GetVersions(dataset.Id))?.FirstOrDefault(dh => dh.Version == datasetVersion)?.BlobName;

            return await DownloadDatasetFile(currentDatasetId, fullBlobName, datasetVersion);
        }

        public async Task<IActionResult> DownloadOriginalDatasetUploadFile(string datasetId, string datasetVersionStr)
        {
            int datasetVersion = -1;

            if (!string.IsNullOrWhiteSpace(datasetVersionStr))
            {
                bool successfullyParsed = int.TryParse(datasetVersionStr, out datasetVersion);
                if (!successfullyParsed)
                {
                    return new BadRequestObjectResult("Invalid value was provided to datasetVersion");
                }
            }

            if (string.IsNullOrWhiteSpace(datasetId))
            {
                _logger.Error($"No {nameof(datasetId)} was provided to {nameof(DownloadDatasetFile)}");

                return new BadRequestObjectResult($"Null or empty {nameof(datasetId)} provided");
            }

            Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(datasetId);

            if (dataset == null)
            {
                _logger.Error($"A dataset could not be found for dataset id: {datasetId}");

                return new StatusCodeResult(412);
            }

            string fullBlobName = datasetVersion == -1 ? dataset.Current?.UploadedBlobFilePath : (await _versionDatasetRepository.GetVersions(dataset.Id))?.FirstOrDefault(dh => dh.Version == datasetVersion)?.UploadedBlobFilePath;

            return await DownloadDatasetFile(datasetId, fullBlobName, datasetVersion);
        }

        private async Task<IActionResult> DownloadDatasetFile(string datasetId, string fullBlobName, int datasetVersion)
        {
            if (string.IsNullOrWhiteSpace(fullBlobName))
            {
                string errorLog = $"A blob name could not be found for dataset id: {datasetId}";
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

            DatasetDownloadModel downloadModel = new DatasetDownloadModel {Url = blobUrl};

            return new OkObjectResult(downloadModel);
        }

        public async Task<IActionResult> Reindex()
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
                    Description = dataset.Content.Current.Description,
                    Version = dataset.Content.Current.Version,
                    ChangeNote = dataset.Content.Current.Comment,
                    ChangeType = dataset.Content.Current.ChangeType == DatasetChangeType.Unknown ? DatasetChangeType.NewVersion.ToString() : dataset.Content.Current.ChangeType.ToString(),
                    LastUpdatedById = dataset.Content.Current.Author?.Id,
                    FundingStreamId = dataset.Content.Current.FundingStream?.Id,
                    FundingStreamName = dataset.Content.Current.FundingStream?.Name
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

        public async Task<IActionResult> ReindexDatasetVersions()
        {
            IEnumerable<DocumentEntity<Dataset>> datasets = await _datasetRepository.GetDatasets();
            int searchBatchSize = 100;

            int totalInserts = 0;

            List<DatasetVersionIndex> searchEntries = new List<DatasetVersionIndex>(searchBatchSize);

            foreach (DocumentEntity<Dataset> dataset in datasets)
            {
                foreach (DatasetVersion datasetVersion in await _versionDatasetRepository.GetVersions(dataset.Id))
                {
                    DatasetVersionIndex datasetVersionIndex = new DatasetVersionIndex
                    {
                        Id = $"{dataset.Id}-{datasetVersion.Version}",
                        DatasetId = dataset.Id,
                        Name = dataset.Content.Name,
                        Version = datasetVersion.Version,
                        BlobName = datasetVersion.BlobName,
                        ChangeNote = datasetVersion.Comment,
                        ChangeType = datasetVersion.ChangeType.ToString(),
                        DefinitionName = dataset.Content.Definition.Name,
                        Description = datasetVersion.Description,
                        LastUpdatedDate = datasetVersion.Date,
                        LastUpdatedByName = datasetVersion.Author.Name,
                        FundingStreamId = datasetVersion.FundingStream?.Id,
                        FundingStreamName = datasetVersion.FundingStream?.Name
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

        public async Task<IActionResult> RegenerateProviderSourceDatasets(string specificationId, Reference user, string correlationId)
        {
            IEnumerable<DefinitionSpecificationRelationship> relationships;

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                relationships = await _datasetRepository.GetAllDefinitionSpecificationsRelationships();
                ;
            }
            else
            {
                relationships = await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(r => r.Content.Current.Specification.Id == specificationId);
            }

            Dictionary<string, Dataset> datasets = new Dictionary<string, Dataset>();

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                if (relationship == null || relationship.Current.DatasetVersion == null || string.IsNullOrWhiteSpace(relationship.Current.DatasetVersion.Id))
                {
                    continue;
                }

                if (!datasets.TryGetValue(relationship.Current.DatasetVersion.Id, out Dataset dataset))
                {
                    dataset = (await _datasetRepository.GetDatasetsByQuery(c => c.Id == relationship.Current.DatasetVersion.Id)).FirstOrDefault();
                    datasets.Add(relationship.Current.DatasetVersion.Id, dataset);
                }

                Trigger trigger = new Trigger
                {
                    EntityId = dataset.Id,
                    EntityType = nameof(Dataset),
                    Message = $"Mapping dataset: '{dataset.Id}'"
                };

                JobCreateModel job = new JobCreateModel
                {
                    InvokerUserDisplayName = user?.Name,
                    InvokerUserId = user?.Id,
                    JobDefinitionId = JobConstants.DefinitionNames.MapDatasetJob,
                    MessageBody = JsonConvert.SerializeObject(dataset),
                    Properties = new Dictionary<string, string>
                    {
                        {"specification-id", relationship.Current.Specification.Id},
                        {"relationship-id", relationship.Id},
                        {"session-id", relationship.Current.Specification.Id}
                    },
                    SpecificationId = relationship.Current.Specification.Id,
                    Trigger = trigger,
                    CorrelationId = correlationId
                };

                await _jobManagement.QueueJob(job);
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

            IEnumerable<Dataset> datasets = (await _datasetRepository.GetDatasetsByQuery(m => m.Content.Definition.Id == datsetDefinitionReference.Id)).ToList();

            if (datasets.IsNullOrEmpty())
            {
                _logger.Information($"No datasets found to update for definition id: {datsetDefinitionReference.Id}");

                return;
            }

            foreach (Dataset dataset in datasets)
            {
                dataset.Definition.Name = datsetDefinitionReference.Name;
            }

            try
            {
                await _datasetRepository.SaveDatasets(datasets);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to save datasets to cosmos for definition id: {datsetDefinitionReference.Id}");

                throw new RetriableException($"Failed to save datasets to cosmos for definition id: {datsetDefinitionReference.Id}", ex);
            }

            List<IndexError> indexErrors = new List<IndexError>();

            foreach (Dataset dataset in datasets)
            {
                indexErrors.AddRange((await IndexDatasetInSearch(dataset)));

                foreach (DatasetVersion datasetVersion in await _versionDatasetRepository.GetVersions(dataset.Id))
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

        public async Task DeleteDatasets(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.UserProperties["specification-id"].ToString();
            if (string.IsNullOrEmpty(specificationId))
            {
                string error = "Null or empty specification Id provided for deleting datasets";
                _logger.Error(error);
                throw new Exception(error);
            }

            string deletionTypeProperty = message.UserProperties["deletion-type"].ToString();
            if (string.IsNullOrEmpty(deletionTypeProperty))
            {
                string error = "Null or empty deletion type provided for deleting datasets";
                _logger.Error(error);
                throw new Exception(error);
            }

            DeletionType deletionType = deletionTypeProperty.ToDeletionType();

            SpecificationSummary specificationSummary;
            ApiResponse<SpecificationSummary> specificationSummaryApiResponse =
                await _specificationsApiClient.GetSpecificationSummaryById(specificationId);

            if (specificationSummaryApiResponse.StatusCode == HttpStatusCode.OK)
            {
                specificationSummary = specificationSummaryApiResponse.Content;
            }
            else
            {
                string error = $"There was an issue with retrieving specification '{specificationId}'";
                _logger.Error(error);
                throw new Exception(error);
            }

            foreach (string id in specificationSummary.DataDefinitionRelationshipIds)
            {
                await _providerSourceDatasetRepository.DeleteProviderSourceDatasetVersion(id, deletionType);

                await _providerSourceDatasetRepository.DeleteProviderSourceDataset(id, deletionType);
            }

            await _datasetRepository.DeleteDefinitionSpecificationRelationshipBySpecificationId(specificationId, deletionType);

            await _datasetRepository.DeleteDatasetsBySpecificationId(specificationId, deletionType);
        }

        private async Task<Dataset> SaveNewDatasetAndVersion(
            ICloudBlob blob,
            DatasetDefinition datasetDefinition,
            int rowCount,
            string uploadedBlobFilePath,
            PoliciesApiModels.FundingStream fundingStream)
        {
            Guard.ArgumentNotNull(blob, nameof(blob));
            Guard.ArgumentNotNull(datasetDefinition, nameof(datasetDefinition));
            Guard.ArgumentNotNull(rowCount, nameof(rowCount));

            IDictionary<string, string> metadata = blob.Metadata;

            Guard.ArgumentNotNull(metadata, nameof(metadata));

            DatasetMetadataModel metadataModel = new DatasetMetadataModel
            {
                AuthorName = metadata.ContainsKey("authorName") ? metadata["authorName"] : string.Empty,
                AuthorId = metadata.ContainsKey("authorId") ? metadata["authorId"] : string.Empty,
                DatasetId = metadata.ContainsKey("datasetId") ? metadata["datasetId"] : string.Empty,
                DataDefinitionId = metadata.ContainsKey("dataDefinitionId") ? metadata["dataDefinitionId"] : string.Empty,
                Name = metadata.ContainsKey("name") ? metadata["name"] : string.Empty,
                Description = metadata.ContainsKey("description") ? HttpUtility.UrlDecode(metadata["description"]) : string.Empty,
                Comment = metadata.ContainsKey("comment") ? metadata["comment"] : string.Empty,
                FundingStreamId = metadata.ContainsKey("fundingStreamId") ? metadata["fundingStreamId"] : string.Empty,
            };

            ValidationResult validationResult = await _datasetMetadataModelValidator.ValidateAsync(metadataModel);

            if (!validationResult.IsValid)
            {
                _logger.Error($"Invalid metadata on blob: {blob.Name}");

                throw new Exception($"Invalid metadata on blob: {blob.Name}");
            }

            DatasetVersion newVersion = new DatasetVersion
            {
                DatasetId = metadataModel.DatasetId,
                Author = new Reference(metadataModel.AuthorId, metadataModel.AuthorName),
                Version = 1,
                Date = DateTimeOffset.Now,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                BlobName = blob.Name,
                UploadedBlobFilePath = uploadedBlobFilePath,
                ChangeType = DatasetChangeType.NewVersion,
                RowCount = rowCount,
                Comment = metadataModel.Comment,
                Description = metadataModel.Description,
                FundingStream = fundingStream
            };

            Dataset dataset = new Dataset
            {
                Id = metadataModel.DatasetId,
                Name = metadataModel.Name,
                Definition = new DatasetDefinitionVersion
                {
                    Id = datasetDefinition.Id,
                    Name = datasetDefinition.Name,
                    Version = datasetDefinition.Version
                },
                Current = newVersion
            };

            await _versionDatasetRepository.SaveVersion(newVersion);

            HttpStatusCode statusCode = await _datasetRepository.SaveDataset(dataset);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save dataset for id: {metadataModel.DatasetId} with status code {statusCode}");

                throw new InvalidOperationException($"Failed to save dataset for id: {metadataModel.DatasetId} with status code {statusCode}");
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

        private async Task<Dataset> UpdateExistingDatasetAndAddVersion(
            ICloudBlob blob,
            GetDatasetBlobModel model,
            Reference author,
            int rowCount,
            string uploadedBlobFilePath,
            PoliciesApiModels.FundingStream fundingStream,
            DatasetDataMergeResult mergeResult)
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

            int newVersionNumber = await _versionDatasetRepository.GetNextVersionNumber(dataset.Current);

            if (model.Version != newVersionNumber)
            {
                _logger.Error($"Failed to save dataset or dataset version for id: {model.DatasetId} due to version mismatch. Expected next version to be {newVersionNumber} but request provided '{model.Version}'");

                throw new InvalidOperationException($"Failed to save dataset or dataset version for id: {model.DatasetId} due to version mismatch. Expected next version to be {newVersionNumber} but request provided '{model.Version}'");
            }

            DatasetVersion newVersion = new DatasetVersion
            {
                DatasetId = dataset.Id,
                Author = new Reference(author.Id, author.Name),
                Version = model.Version,
                Date = DateTimeOffset.Now,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                BlobName = blob.Name,
                UploadedBlobFilePath = uploadedBlobFilePath,
                Comment = model.Comment,
                RowCount = rowCount,
                FundingStream = fundingStream,
                NewRowCount = mergeResult.TotalRowsCreated,
                AmendedRowCount = mergeResult.TotalRowsAmended,
                ChangeType = model.Version > 1 && model.MergeExistingVersion ? DatasetChangeType.Merge : DatasetChangeType.NewVersion,
                ProviderVersionId = null,
                Description = model.Description
            };

            dataset.Current = newVersion;
            await _versionDatasetRepository.SaveVersion(newVersion);

            HttpStatusCode statusCode = await _datasetRepository.SaveDataset(dataset);

            if (!statusCode.IsSuccess())
            {
                _logger.Warning($"Failed to save dataset for id: {model.DatasetId} with status code {statusCode}");

                throw new InvalidOperationException($"Failed to save dataset for id: {model.DatasetId} with status code {statusCode}");
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
                    DefinitionId = dataset.Definition?.Id,
                    DefinitionName = dataset.Definition?.Name,
                    Status = dataset.Current.PublishStatus.ToString(),
                    LastUpdatedDate = DateTimeOffset.Now,
                    Description = dataset.Current.Description,
                    Version = dataset.Current.Version,
                    ChangeNote = dataset.Current.Comment,
                    ChangeType = dataset.Current.ChangeType.ToString(),
                    LastUpdatedById = dataset.Current.Author?.Id,
                    LastUpdatedByName = dataset.Current.Author?.Name,
                    FundingStreamId = dataset.Current.FundingStream?.Id,
                    FundingStreamName = dataset.Current.FundingStream?.Name,
                    RelationshipId = dataset.RelationshipId
                }
            });
        }

        private Task<IEnumerable<IndexError>> IndexDatasetVersionInSearch(Dataset dataset, DatasetVersion datasetVersion)
        {
            Guard.ArgumentNotNull(dataset, nameof(dataset));

            return _datasetVersionIndexRepository.Index(new List<DatasetVersionIndex>
            {
                new DatasetVersionIndex
                {
                    Id = $"{dataset.Id}-{datasetVersion.Version}",
                    DatasetId = dataset.Id,
                    Name = dataset.Name,
                    Version = datasetVersion.Version,
                    BlobName = datasetVersion.BlobName,
                    ChangeNote = datasetVersion.Comment,
                    ChangeType = datasetVersion.ChangeType.ToString(),
                    DefinitionName = dataset.Definition?.Name,
                    Description = datasetVersion.Description,
                    LastUpdatedDate = datasetVersion.Date,
                    LastUpdatedByName = datasetVersion.Author.Name,
                    FundingStreamId = datasetVersion.FundingStream?.Id,
                    FundingStreamName = datasetVersion.FundingStream?.Name
                }
            });
        }

        private async Task<(IDictionary<string, IEnumerable<string>> validationFailures, int providersProcessed)> ValidateTableResults(
            DatasetDefinition datasetDefinition, 
            Stream datasetStream,
            DatasetEmptyFieldEvaluationOption datasetEmptyFieldEvaluationOption,
            string currentBlobName)
        {
            int rowCount = 0;
            Dictionary<string, IEnumerable<string>> validationFailures = new Dictionary<string, IEnumerable<string>>();

            ConcurrentBag<ProviderSummary> summaries = new ConcurrentBag<ProviderSummary>();

            ApiResponse<ApiClientProviders.Models.ProviderVersion> providerVersionResponse
                = await _providersApiClientPolicy.ExecuteAsync(() =>
                    _providersApiClient.GetCurrentProvidersForFundingStream(datasetDefinition.FundingStreamId));

            if (!providerVersionResponse.StatusCode.IsSuccess() && providerVersionResponse.StatusCode != HttpStatusCode.NotFound)
            {
                string errorMessage = $"Failed to fetch current providers for funding stream {datasetDefinition.FundingStreamId} with status code: {providerVersionResponse.StatusCode}";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            if (providerVersionResponse.StatusCode == HttpStatusCode.NotFound || providerVersionResponse.Content == null || providerVersionResponse.Content.Providers.IsNullOrEmpty())
            {
                _logger.Error($"No provider version for the funding stream {datasetDefinition.FundingStreamId}");
                validationFailures.Add(nameof(datasetDefinition.FundingStreamId), new string[] {$"No provider version for the funding stream {datasetDefinition.FundingStreamId}"});

                return (validationFailures, rowCount);
            }

            Parallel.ForEach(providerVersionResponse.Content.Providers, (provider) => { summaries.Add(_mapper.Map<ProviderSummary>(provider)); });

            using ExcelPackage excelPackage = new ExcelPackage(datasetStream);
            DatasetUploadValidationModel uploadModel = new DatasetUploadValidationModel(
                excelPackage, 
                () => summaries, 
                datasetDefinition, 
                datasetEmptyFieldEvaluationOption,
                currentBlobName);
            ValidationResult validationResult = await _datasetUploadValidator.ValidateAsync(uploadModel);
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

                await FailValidation(excelPackage.Stream);

                validationFailures.Add("excel-validation-error", new string[] {string.Empty});
                validationFailures.Add("error-message", new string[] {"The data source file does not match the schema rules"});
            }

            return (validationFailures, rowCount);
        }

        public async Task<IActionResult> GetCurrentDatasetVersionByDatasetId(string datasetId)
        {
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
                Description = dataset.Content.Current.Description,
                LastUpdatedDate = dataset.UpdatedAt,
                Name = dataset.Content.Name,
                PublishStatus = dataset.Content.Current.PublishStatus,
                Version = dataset.Content.Current.Version,
                Comment = dataset.Content.Current.Comment,
                CurrentDataSourceRows = dataset.Content.Current.RowCount,
                FundingStream = dataset.Content.Current.FundingStream,
                AmendedRowCount = dataset.Content.Current.AmendedRowCount,
                NewRowCount = dataset.Content.Current.NewRowCount
            };

            int maxVersion = dataset.Content.Current.Version;
            if (maxVersion > 1)
            {
                result.PreviousDataSourceRows = (await _versionDatasetRepository.GetVersion(dataset.Id, maxVersion - 1)).RowCount;
            }

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> FixupDatasetsFundingStream()
        {
            int totalUpdatedDatasets = 0;
            IEnumerable<Dataset> datasetDocuments = await _datasetRepository.GetDatasetsByQuery(_ => !_.Content.Current.FundingStream.IsDefined() || !_.Content.Definition.Version.IsDefined());

            if (datasetDocuments.AnyWithNullCheck())
            {
                foreach (Dataset dataset in datasetDocuments)
                {
                    DatasetDefinition definition = await _datasetRepository.GetDatasetDefinition(dataset.Definition.Id);

                    if (dataset.Current.FundingStream == null)
                    {
                        dataset.Current.FundingStream = new Reference {Id = definition.FundingStreamId, Name = definition.FundingStreamId};
                    }

                    // version has never been set so need to set it here
                    if (!dataset.Definition.Version.HasValue)
                    {
                        dataset.Definition.Version = definition.Version;
                    }

                    totalUpdatedDatasets++;
                }

                await _datasetRepository.SaveDatasets(datasetDocuments);
            }

            return new OkObjectResult($"Migrated total of {totalUpdatedDatasets} Datasets");
        }

        public async Task<IActionResult> MigrateDatasetsPerDocumentVersioning()
        {
            int totalUpdatedDatasets = 0;
            IEnumerable<DocumentEntity<OldDataset>> oldDatasetDocuments = await _datasetRepository.GetOldDatasetsToMigrate();

            foreach (DocumentEntity<OldDataset> dataset in oldDatasetDocuments)
            {
                if (dataset.Content.History != null)
                {
                    await _versionDatasetRepository.SaveVersions(dataset.Content.History?.Select(_ =>
                    {
                        _.DatasetId = dataset.Content.Id;
                        _.Description = dataset.Content.Description;

                        return _;
                    }));
                }

                dataset.Content.Current.DatasetId = dataset.Content.Id;

                await _datasetRepository.SaveDataset(new Dataset
                {
                    Current = dataset.Content.Current,
                    Definition = dataset.Content.Definition,
                    RelationshipId = dataset.Content.RelationshipId,
                    Name = dataset.Content.Name,
                    Id = dataset.Content.Id
                });

                totalUpdatedDatasets++;
            }

            return new OkObjectResult($"Migrated total of {totalUpdatedDatasets} Datasets");
        }

        public IActionResult GetValidateDatasetValidationErrorSasUrl(DatasetValidationErrorRequestModel requestModel)
        {
            if (requestModel == null)
            {
                _logger.Warning("No dataset validation error request model was provided");
                return new BadRequestObjectResult("No dataset validation error request model was provided");
            }

            if (requestModel.JobId.IsNullOrEmpty())
            {
                _logger.Warning("No job id was provided");
                return new BadRequestObjectResult("No job id was provided");
            }

            string blobName = $"validation-errors/{requestModel.JobId}.xlsx";

            string blobUrl = _blobClient.GetBlobSasUrl(blobName, DateTimeOffset.UtcNow.AddDays(1), SharedAccessBlobPermissions.Read);

            return new OkObjectResult(new DatasetValidationErrorSasUrlResponseModel {ValidationErrorFileUrl = blobUrl});
        }
        
        private string GetUploadedBlobFilepath(string filename, string datasetId, int version = 1)
        {
            string nameWithoutExtension = filename.Substring(0, filename.LastIndexOf('.'));
            string fileExtension = filename.Substring(filename.LastIndexOf('.'));

            return $"{datasetId}/v{version}/{nameWithoutExtension}.uploaded{fileExtension}";
        }

        private string GetMergedBlobFilepath(string filename, string datasetId, int version = 1)
        {
            string nameWithoutExtension = filename.Substring(0, filename.LastIndexOf('.'));
            string fileExtension = filename.Substring(filename.LastIndexOf('.'));

            return $"{datasetId}/v{version}/{nameWithoutExtension}.v{version}{fileExtension}";
        }
    }
}