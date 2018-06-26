using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using Polly;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetService : IDatasetService
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
        private readonly ServiceBusSettings _eventHubSettings;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IExcelDatasetReader _excelDatasetReader;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICalcsRepository _calcsRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly IProvidersResultsRepository _providersResultsRepository;
        private readonly ITelemetry _telemetry;

        private readonly Policy _providerResultsRepositoryPolicy;

        const string dataset_cache_key_prefix = "ds-table-rows";

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
            ISpecificationsRepository specificationsRepository,
            IMessengerService messengerService,
            ServiceBusSettings eventHubSettings,
            IExcelDatasetReader excelDatasetReader,
            ICacheProvider cacheProvider,
            ICalcsRepository calcsRepository,
            IProviderRepository providerRepository,
            IProvidersResultsRepository providersResultsRepository,
            ITelemetry telemetry,
            IDatasetsResiliencePolicies datasetsResiliencePolicies)
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
            _eventHubSettings = eventHubSettings;
            _specificationsRepository = specificationsRepository;
            _excelDatasetReader = excelDatasetReader;
            _cacheProvider = cacheProvider;
            _calcsRepository = calcsRepository;
            _providerRepository = providerRepository;
            _providersResultsRepository = providersResultsRepository;
            _telemetry = telemetry;

            Guard.ArgumentNotNull(datasetsResiliencePolicies, nameof(datasetsResiliencePolicies));

            _providerResultsRepositoryPolicy = datasetsResiliencePolicies.ProviderResultsRepository;
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
                return validationResult;

            string version = "v1";

            string datasetId = Guid.NewGuid().ToString();

            string fileName = $"{datasetId}/{version}/{model.Filename}";

            string blobUrl = _blobClient.GetBlobSasUrl(fileName,
                DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);

            NewDatasetVersionResponseModel responseModel = _mapper.Map<NewDatasetVersionResponseModel>(model);

            responseModel.DatasetId = datasetId;
            responseModel.BlobUrl = blobUrl;
            responseModel.Author = request.GetUser();
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
                return validationResult;

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

        async public Task<IActionResult> ValidateDataset(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            GetDatasetBlobModel model = JsonConvert.DeserializeObject<GetDatasetBlobModel>(json);

            if (model == null)
            {
                _logger.Error("Null model name was provided to ValidateDataset");
                return new BadRequestObjectResult("Null model name was provided");
            }

            var validationResult = (await _getDatasetBlobModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            string fullBlobName = model.ToString();

            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);

            if (blob == null)
            {
                _logger.Error($"Failed to find blob with path: {fullBlobName}");
                return new StatusCodeResult(412);
            }

            await blob.FetchAttributesAsync();

            string dataDefinitionId = blob.Metadata["dataDefinitionId"];

            DatasetDefinition datasetDefinition =
                (await _datasetRepository.GetDatasetDefinitionsByQuery(m => m.Id == dataDefinitionId)).FirstOrDefault();

            if (datasetDefinition == null)
            {
                _logger.Error($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");

                return new StatusCodeResult(412);
            }

            IActionResult actionResult = await ValidateTableResults(datasetDefinition, blob);

            if (actionResult is OkResult)
            {
                try
                {
                    if (model.Version == 1)
                    {
                        await SaveNewDatasetAndVersion(blob, datasetDefinition);
                    }
                    else
                    {
                        await UpdateExistingDatasetAndAddVersion(blob, model, request.GetUser());
                    }
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Failed to save the dataset or dataset version");
                    return new InternalServerErrorResult($"Failed to save the dataset or dataset version. {exception.Message}");
                }
            }

            return actionResult;
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

            try
            {
                buildProject = await ProcessDataset(dataset, specificationId, relationshipId, relationship.DatasetVersion.Version);
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

            try
            {
                buildProject = await ProcessDataset(dataset, specificationId, relationshipId, relationship.DatasetVersion.Version);
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
            IEnumerable<DefinitionSpecificationRelationship> relationships = await _datasetRepository.GetAllDefinitionSpecificationsRelationships();

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

                IDictionary<string, string> properties = new Dictionary<string, string>();
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
                            var lookup = GetDictionaryForIdentifierType(field.IdentifierFieldType, identifier);
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

        private static Dictionary<string, List<string>> GetDictionaryForIdentifierType(IdentifierFieldType? identifierFieldType, string fieldIdentifier)
        {
            var identifierMaps = new Dictionary<IdentifierFieldType, Dictionary<string, List<string>>>();

            if (!identifierFieldType.HasValue)
            {
                return new Dictionary<string, List<string>>();
            }

            Dictionary<IdentifierFieldType, Dictionary<string, List<string>>> identifiers = new Dictionary<IdentifierFieldType, Dictionary<string, List<string>>>();

            Func<ProviderSummary, string> identifierSelectorExpression = GetIdentifierSelectorExpression(identifierFieldType.Value);

            IEnumerable<string> filteredIdentifiers = _providerSummaries.Select(identifierSelectorExpression);

            identifiers.Add(identifierFieldType.Value, new Dictionary<string, List<string>> { { fieldIdentifier, filteredIdentifiers.ToList() } });

            return identifiers[identifierFieldType.Value];
        }

        async Task SaveNewDatasetAndVersion(ICloudBlob blob, DatasetDefinition datasetDefinition)
        {
            Guard.ArgumentNotNull(blob, nameof(blob));
            Guard.ArgumentNotNull(datasetDefinition, nameof(datasetDefinition));

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
                BlobName = blob.Name
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

                throw new Exception($"Failed to save dataset for id: {metadataModel.DatasetId} with status code {statusCode.ToString()}");
            }

            IEnumerable<IndexError> indexErrors = await IndexDatasetInSearch(dataset);

            if (indexErrors.Any())
            {
                string errors = string.Join(";", indexErrors.Select(m => m.ErrorMessage).ToArraySafe());

                _logger.Error($"Failed to save dataset for id: {metadataModel.DatasetId} in search with errors {errors}");

                throw new Exception($"Failed to save dataset for id: {metadataModel.DatasetId} in search with errors {errors}");
            }
        }

        private async Task UpdateExistingDatasetAndAddVersion(ICloudBlob blob, GetDatasetBlobModel model, Reference author)
        {
            Guard.ArgumentNotNull(blob, nameof(blob));

            IDictionary<string, string> metadata = blob.Metadata;

            Guard.ArgumentNotNull(metadata, nameof(metadata));

            Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(model.DatasetId);
            if (dataset == null)
            {
                _logger.Warning($"Failed to retrieve dataset for id: {model.DatasetId} response was null");

                throw new Exception($"Failed to retrieve dataset for id: {model.DatasetId} response was null");
            }

            if (model.Version != dataset.GetNextVersion())
            {
                _logger.Error($"Failed to save dataset for id: {model.DatasetId} due to version mismatch. Expected next version to be {dataset.GetNextVersion()} but request provided '{model.Version}'");

                throw new Exception($"Failed to save dataset for id: {model.DatasetId} due to version mismatch. Expected next version to be {dataset.GetNextVersion()} but request provided '{model.Version}'");
            }

            DatasetVersion newVersion = new DatasetVersion
            {
                Author = new Reference(author.Id, author.Name),
                Version = model.Version,
                Date = DateTimeOffset.Now,
                PublishStatus = PublishStatus.Draft,
                BlobName = blob.Name,
                Commment = model.Comment,
            };

            dataset.Description = model.Description;
            dataset.Current = newVersion;
            dataset.History.Add(newVersion);

            HttpStatusCode statusCode = await _datasetRepository.SaveDataset(dataset);

            if (!statusCode.IsSuccess())
            {
                _logger.Warning($"Failed to save dataset for id: {model.DatasetId} with status code {statusCode.ToString()}");

                throw new Exception($"Failed to save dataset for id: {model.DatasetId} with status code {statusCode.ToString()}");
            }

            IEnumerable<IndexError> indexErrors = await IndexDatasetInSearch(dataset);

            if (indexErrors.Any())
            {
                string errors = string.Join(";", indexErrors.Select(m => m.ErrorMessage).ToArraySafe());

                _logger.Warning($"Failed to save dataset for id: {model.DatasetId} in search with errors {errors}");

                throw new Exception($"Failed to save dataset for id: {model.DatasetId} in search with errors {errors}");
            }
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
                return x => x.URN;

            else if (identifierFieldType == IdentifierFieldType.Authority)
                return x => x.Authority;

            else if (identifierFieldType == IdentifierFieldType.EstablishmentNumber)
                return x => x.EstablishmentNumber;

            else if (identifierFieldType == IdentifierFieldType.UKPRN)
                return x => x.UKPRN;

            else if (identifierFieldType == IdentifierFieldType.UPIN)
                return x => x.UPIN;

            else
                return null;
        }

        private async Task<IActionResult> ValidateTableResults(DatasetDefinition datasetDefinition, ICloudBlob blob)
        {
            string dataset_cache_key = $"{dataset_cache_key_prefix}:{blob.Name}:{datasetDefinition.Id}";

            IEnumerable<TableLoadResult> tableLoadResults = await _cacheProvider.GetAsync<TableLoadResult[]>(dataset_cache_key);

            if (tableLoadResults.IsNullOrEmpty())
            {
                var datasetStream = await _blobClient.DownloadToStreamAsync(blob);

                if (datasetStream.Length == 0)
                {
                    _logger.Error($"Blob {blob.Name} contains no data");
                    return new StatusCodeResult(412);
                }

                tableLoadResults = _excelDatasetReader.Read(datasetStream, datasetDefinition).ToList();
            }

            var validationErrors = new List<DatasetValidationError>();

            foreach (var tableLoadResult in tableLoadResults)
            {
                if (tableLoadResult.GlobalErrors.Any())
                {
                    validationErrors.AddRange(tableLoadResult.GlobalErrors);
                }
            }

            if (validationErrors.Any())
            {
                int errorCount = validationErrors.Count;

                return new OkObjectResult(new DatasetValidationErrorResponse
                {
                    Message = $"The dataset failed to validate with {errorCount} {(errorCount == 1 ? "error" : "errors")}",
                    FileUrl = "http://anyUrl"
                });
            }

            await _cacheProvider.SetAsync(dataset_cache_key, tableLoadResults.ToArraySafe(), TimeSpan.FromDays(1), false);

            return new OkResult();
        }

        async Task<BuildProject> ProcessDataset(Dataset dataset, string specificationId, string relationshipId, int version)
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

            await PersistDataset(loadResult, dataset, datasetDefinition, buildProject, specificationId, relationshipId, version);

            return buildProject;
        }

        async Task PersistDataset(TableLoadResult loadResult, Dataset dataset, DatasetDefinition datasetDefinition, BuildProject buildProject, string specificationId, string relationshipId, int version)
        {
            if (_providerSummaries.IsNullOrEmpty())
                _providerSummaries = await _providerRepository.GetAllProviderSummaries();

            Guard.IsNullOrWhiteSpace(relationshipId, nameof(relationshipId));

            IList<ProviderSourceDatasetCurrent> providerSourceDatasets = new List<ProviderSourceDatasetCurrent>();

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


            Dictionary<string, ProviderSourceDatasetCurrent> resultsByProviderId = new Dictionary<string, ProviderSourceDatasetCurrent>();

            Dictionary<string, ProviderSourceDatasetCurrent> existingCurrent = new Dictionary<string, ProviderSourceDatasetCurrent>();

            Dictionary<string, ProviderSourceDatasetHistory> existingHistory = new Dictionary<string, ProviderSourceDatasetHistory>();

            Dictionary<string, ProviderSourceDatasetHistory> updateDatasetsHistory = new Dictionary<string, ProviderSourceDatasetHistory>();

            Dictionary<string, ProviderSourceDatasetCurrent> updateCurrentDatasets = new Dictionary<string, ProviderSourceDatasetCurrent>();

            IEnumerable<ProviderSourceDatasetCurrent> existingCurrentDatasets = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.GetCurrentProviderSourceDatasets(specificationId, relationshipId));

            IEnumerable<ProviderSourceDatasetHistory> existingDatasetHistory = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.GetProviderSourceDatasetHistories(specificationId, relationshipId));

            if (existingCurrentDatasets.AnyWithNullCheck())
            {
                foreach (ProviderSourceDatasetCurrent currentDataset in existingCurrentDatasets)
                {
                    existingCurrent.Add(currentDataset.ProviderId, currentDataset);
                }
            }

            if (existingDatasetHistory.AnyWithNullCheck())
            {
                foreach (ProviderSourceDatasetHistory datasetHistory in existingDatasetHistory)
                {
                    existingHistory.Add(datasetHistory.Provider.Id, datasetHistory);
                }
            }

            foreach (RowLoadResult row in loadResult.Rows)
            {
                IEnumerable<string> allProviderIds = GetProviderIdsForIdentifier(datasetDefinition, row);

                IEnumerable<string> providerIds = allProviderIds.Where(x => x == row.Identifier).ToList();

                foreach (string providerId in providerIds)
                {
                    if (!resultsByProviderId.TryGetValue(providerId, out ProviderSourceDatasetCurrent sourceDataset))
                    {
                        sourceDataset = new ProviderSourceDatasetCurrent
                        {
                            DataGranularity = relationshipSummary.DataGranularity,
                            SpecificationId = specificationId,
                            DefinesScope = relationshipSummary.DefinesScope,
                            DataDefinition = new Reference(relationshipSummary.DatasetDefinition.Id, relationshipSummary.DatasetDefinition.Name),
                            DataRelationship = new Reference(relationshipSummary.Relationship.Id, relationshipSummary.Relationship.Name),
                            DatasetRelationshipSummary = new Reference(relationshipSummary.Id, relationshipSummary.Name),
                            ProviderId = providerId,
                            Rows = new List<Dictionary<string, object>>()
                        };

                        resultsByProviderId.Add(providerId, sourceDataset);
                    }

                    sourceDataset.Rows.Add(row.Fields);
                }
            }

            foreach (var providerSourceDataset in resultsByProviderId)
            {
                string providerId = providerSourceDataset.Key;
                ProviderSourceDatasetCurrent sourceDataset = providerSourceDataset.Value;

                if (existingCurrent.ContainsKey(providerId))
                {
                    string existingDatasetJson = JsonConvert.SerializeObject(existingCurrent[providerId]);
                    string latestDatasetJson = JsonConvert.SerializeObject(sourceDataset);

                    if (existingDatasetJson != latestDatasetJson)
                    {
                        updateCurrentDatasets.Add(providerId, sourceDataset);
                    }
                }
                else
                {
                    updateCurrentDatasets.Add(providerId, sourceDataset);
                }

                ProviderSourceDatasetHistory datasetHistory = null;
                if (existingHistory.TryGetValue(providerId, out datasetHistory))
                {
                    // Only update if the row values change
                    string existingSourceDatasetJson = JsonConvert.SerializeObject(datasetHistory.History?.LastOrDefault().Rows);

                    string currentSourceDatasetJson = JsonConvert.SerializeObject(providerSourceDataset.Value.Rows);

                    if (existingSourceDatasetJson != currentSourceDatasetJson)
                    {
                        SourceDataset latestSourceDataset = new SourceDataset
                        {
                            Dataset = new VersionReference(dataset.Id, dataset.Name, version),
                            Rows = providerSourceDataset.Value.Rows,
                            Date = DateTimeOffset.Now,
                            Version = datasetHistory.GetNextVersion(),
                        };

                        datasetHistory.History = datasetHistory.History.Concat(new[] { latestSourceDataset });

                        updateDatasetsHistory.Add(providerId, datasetHistory);
                    }
                }
                else
                {
                    datasetHistory = new ProviderSourceDatasetHistory()
                    {
                        DataGranularity = relationshipSummary.DataGranularity,
                        SpecificationId = specificationId,
                        DefinesScope = relationshipSummary.DefinesScope,
                        DataDefinition = new Reference(relationshipSummary.DatasetDefinition.Id, relationshipSummary.DatasetDefinition.Name),
                        DataRelationship = new Reference(relationshipSummary.Relationship.Id, relationshipSummary.Relationship.Name),
                        DatasetRelationshipSummary = new Reference(relationshipSummary.Id, relationshipSummary.Name),
                        Provider = new Reference { Id = providerId },
                        History = new List<SourceDataset>() {
                            new SourceDataset
                            {
                                Dataset  = new VersionReference(dataset.Id, dataset.Name, version),
                                Rows = providerSourceDataset.Value.Rows,
                                Date = DateTimeOffset.Now,
                                Version = 1,
                            }
                        }
                    };

                    updateDatasetsHistory.Add(providerId, datasetHistory);
                }
            }


            await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.UpdateCurrentProviderSourceDatasets(updateCurrentDatasets.Values, specificationId));

            await _providerResultsRepositoryPolicy.ExecuteAsync(() => 
            _providersResultsRepository.UpdateProviderSourceDatasetHistory(updateDatasetsHistory.Values, specificationId));

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
            string dataset_cache_key = $"{dataset_cache_key_prefix}:{fullBlobName}:{datasetDefinition.Id}";

            IEnumerable<TableLoadResult> tableLoadResults = await _cacheProvider.GetAsync<TableLoadResult[]>(dataset_cache_key);

            if (tableLoadResults.IsNullOrEmpty())
            {
                ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);

                if (blob == null)
                {
                    _logger.Error($"Failed to find blob with path: {fullBlobName}");
                    throw new ArgumentException($"Failed to find blob with path: {fullBlobName}");
                }

                await blob.FetchAttributesAsync();
                var datasetStream = await _blobClient.DownloadToStreamAsync(blob);

                if (datasetStream == null || datasetStream.Length == 0)
                {
                    _logger.Error($"Invalid blob returned: {fullBlobName}");
                    throw new ArgumentException($"Invalid blob returned: {fullBlobName}");
                }

                tableLoadResults = _excelDatasetReader.Read(datasetStream, datasetDefinition).ToList();
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
            };

            return new OkObjectResult(result);
        }
    }
}
