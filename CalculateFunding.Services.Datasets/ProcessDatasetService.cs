using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using Serilog;
using static CalculateFunding.Services.Core.Constants.JobConstants;
using AggregatedField = CalculateFunding.Models.Datasets.AggregatedField;
using AggregatedType = CalculateFunding.Models.Datasets.AggregatedTypes;
using VersionReference = CalculateFunding.Models.VersionReference;

namespace CalculateFunding.Services.Datasets
{
    public class ProcessDatasetService : IProcessDatasetService, IHealthChecker
    {
        private readonly IDatasetRepository _datasetRepository;
        private readonly IExcelDatasetReader _excelDatasetReader;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICalcsRepository _calcsRepository;
        private readonly IBlobClient _blobClient;
        private readonly IMessengerService _messengerService;
        private readonly IProviderSourceDatasetsRepository _providersResultsRepository;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly ISpecificationsApiClient _specsApiClient;
        private readonly IVersionRepository<ProviderSourceDatasetVersion> _sourceDatasetsVersionRepository;
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly AsyncPolicy _providerResultsRepositoryPolicy;
        private readonly IDatasetsAggregationsRepository _datasetsAggregationsRepository;
        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly IFeatureToggle _featureToggle;
        private readonly AsyncPolicy _jobsApiClientPolicy;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly IMapper _mapper;
        private readonly IJobManagement _jobManagement;
        private readonly IProviderSourceDatasetVersionKeyProvider _datasetVersionKeyProvider;

        public ProcessDatasetService(
            IDatasetRepository datasetRepository,
            IExcelDatasetReader excelDatasetReader,
            ICacheProvider cacheProvider,
            ICalcsRepository calcsRepository,
            IBlobClient blobClient,
            IMessengerService messengerService,
            IProviderSourceDatasetsRepository providersResultsRepository,
            IProvidersApiClient providersApiClient,
            ISpecificationsApiClient specificationsApiClient,
            IVersionRepository<ProviderSourceDatasetVersion> sourceDatasetsVersionRepository,
            ILogger logger,
            ITelemetry telemetry,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IDatasetsAggregationsRepository datasetsAggregationsRepository,
            IFeatureToggle featureToggle,
            IJobsApiClient jobsApiClient,
            IMapper mapper,
            IJobManagement jobManagement,
            IProviderSourceDatasetVersionKeyProvider datasetVersionKeyProvider)
        {
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(excelDatasetReader, nameof(excelDatasetReader));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(providersResultsRepository, nameof(providersResultsRepository));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(datasetsAggregationsRepository, nameof(datasetsAggregationsRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.ProviderResultsRepository, nameof(datasetsResiliencePolicies.ProviderResultsRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.JobsApiClient, nameof(datasetsResiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.ProvidersApiClient, nameof(datasetsResiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(datasetVersionKeyProvider, nameof(datasetVersionKeyProvider));

            _datasetRepository = datasetRepository;
            _excelDatasetReader = excelDatasetReader;
            _cacheProvider = cacheProvider;
            _calcsRepository = calcsRepository;
            _blobClient = blobClient;
            _providersResultsRepository = providersResultsRepository;
            _providersApiClient = providersApiClient;
            _specsApiClient = specificationsApiClient;
            _sourceDatasetsVersionRepository = sourceDatasetsVersionRepository;
            _logger = logger;
            _telemetry = telemetry;
            _providerResultsRepositoryPolicy = datasetsResiliencePolicies.ProviderResultsRepository;
            _datasetsAggregationsRepository = datasetsAggregationsRepository;
            _providersApiClientPolicy = datasetsResiliencePolicies.ProvidersApiClient;
            _featureToggle = featureToggle;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = datasetsResiliencePolicies.JobsApiClient;
            _messengerService = messengerService;
            _mapper = mapper;
            _jobManagement = jobManagement;
            _datasetVersionKeyProvider = datasetVersionKeyProvider;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_datasetRepository).IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();
            ServiceHealth providersResultsRepoHealth = await ((IHealthChecker)_providersResultsRepository).IsHealthOk();
            ServiceHealth datasetsAggregationsRepoHealth = await ((IHealthChecker)_datasetsAggregationsRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(DatasetService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message });
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });
            health.Dependencies.AddRange(providersResultsRepoHealth.Dependencies);
            health.Dependencies.AddRange(datasetsAggregationsRepoHealth.Dependencies);
            return health;
        }

        public async Task ProcessDataset(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            IDictionary<string, object> properties = message.UserProperties;

            Dataset dataset = message.GetPayloadAsInstanceOf<Dataset>();

            string jobId = message.UserProperties["jobId"].ToString();

            await _jobManagement.UpdateJobStatus(jobId, 0, null);

            if (dataset == null)
            {
                _logger.Error("A null dataset was provided to ProcessData");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, "Failed to Process - null dataset provided");
                return;
            }

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("Specification Id key is missing in ProcessDataset message properties");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, "Failed to Process - specification id not provided");
                return;
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("A null or empty specification id was provided to ProcessData");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, "Failed to Process - specification if is null or empty");
                return;
            }

            if (!message.UserProperties.ContainsKey("relationship-id"))
            {
                _logger.Error("Relationship Id key is missing in ProcessDataset message properties");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, "Failed to Process - relationship id not provided");
                return;
            }

            string relationshipId = message.UserProperties["relationship-id"].ToString();
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                _logger.Error("A null or empty relationship id was provided to ProcessDataset");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, "Failed to Process - relationship id is null or empty");
                return;
            }

            ApiResponse<SpecificationSummary> specificationSummaryResponse = await _specsApiClient.GetSpecificationSummaryById(specificationId);
            if (specificationSummaryResponse == null)
            {
                _logger.Error("Specification summary response was null");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, "Specification summary response was null");
                return;
            }

            if (specificationSummaryResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.Error($"Specification summary returned invalid status code of '{specificationSummaryResponse.StatusCode}'");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, $"Specification summary returned invalid status code of '{specificationSummaryResponse.StatusCode}'");
                return;
            }

            SpecificationSummary specification = specificationSummaryResponse.Content;

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (relationship == null)
            {
                _logger.Error($"Relationship not found for relationship id: {relationshipId}");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, "Failed to Process - relationship not found");
                return;
            }

            BuildProject buildProject = null;

            Reference user = message.GetUserDetails();

            string correlationId = message.GetCorrelationId();

            try
            {
                buildProject = await ProcessDataset(dataset, specification, relationshipId, relationship.DatasetVersion.Version, user, relationship.IsSetAsProviderData, correlationId);

                await _jobManagement.UpdateJobStatus(jobId, 100, true, "Processed Dataset");
            }
            catch (NonRetriableException argEx)
            {
                // This type of exception is not retriable so fail
                _logger.Error(argEx, $"Failed to run ProcessDataset with exception: {argEx.Message} for relationship ID '{relationshipId}'");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, $"Failed to run Process - {argEx.Message}");
                return;
            }
            catch (Exception exception)
            {
                // Unknown exception occurred so allow it to be retried
                _logger.Error(exception, $"Failed to run ProcessDataset with exception: {exception.Message} for relationship ID '{relationshipId}'");
                throw;
            }

            await _datasetVersionKeyProvider.AddOrUpdateProviderSourceDatasetVersionKey(relationshipId, Guid.NewGuid());

            if (buildProject != null && !buildProject.DatasetRelationships.IsNullOrEmpty() && buildProject.DatasetRelationships.Any(m => m.DefinesScope))
            {
                string userId = user != null ? user.Id : "";
                string userName = user != null ? user.Name : "";

                try
                {
                    HttpStatusCode statusCode = await _calcsRepository.CompileAndSaveAssembly(specificationId);

                    if (!statusCode.IsSuccess())
                    {
                        string errorMessage = $"Failed to compile and save assembly for specification id '{specificationId}' with status code '{statusCode}'";

                        _logger.Error(errorMessage);

                        throw new NonRetriableException(errorMessage);
                    }

                    Trigger trigger = new Trigger
                    {
                        EntityId = relationshipId,
                        EntityType = nameof(DefinitionSpecificationRelationship),
                        Message = $"Processed dataset relationship: '{relationshipId}' for specification: '{specificationId}'"
                    };

                    IEnumerable<CalculationResponseModel> allCalculations = await _calcsRepository.GetCurrentCalculationsBySpecificationId(specificationId);

                    bool generateCalculationAggregations = !allCalculations.IsNullOrEmpty() &&
                                                           SourceCodeHelpers.HasCalculationAggregateFunctionParameters(allCalculations.Select(m => m.SourceCode));

                    await SendInstructAllocationsToJobService($"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}", specificationId, userId, userName, trigger, correlationId, generateCalculationAggregations);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'");

                    throw new Exception($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'", ex);
                }
            }
        }

        public async Task<IActionResult> GetDatasetAggregationsBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<DatasetAggregations> aggregates = await _datasetsAggregationsRepository.GetDatasetAggregationsForSpecificationId(specificationId);

            if (aggregates == null)
            {
                aggregates = Array.Empty<DatasetAggregations>();
            }

            return new OkObjectResult(aggregates);
        }

        private DatasetAggregations GenerateAggregations(DatasetDefinition datasetDefinition, TableLoadResult tableLoadResult, string specificationId, Reference datasetRelationship)
        {
            DatasetAggregations datasetAggregations = new DatasetAggregations
            {
                SpecificationId = specificationId,
                DatasetRelationshipId = datasetRelationship.Id,
                Fields = new List<AggregatedField>()
            };

            string identifierPrefix = $"Datasets.{DatasetTypeGenerator.GenerateIdentifier(datasetRelationship.Name)}";

            IEnumerable<FieldDefinition> fieldDefinitions = datasetDefinition.TableDefinitions.SelectMany(m => m.FieldDefinitions);

            RowLoadResult rowLoadResult = tableLoadResult.Rows.FirstOrDefault();

            if (rowLoadResult != null)
            {
                foreach (KeyValuePair<string, object> field in rowLoadResult.Fields)
                {
                    FieldDefinition fieldDefinition = fieldDefinitions.FirstOrDefault(m => m.Name == field.Key);

                    string fieldName = fieldDefinition.Name;

                    if (fieldDefinition.IsAggregable && fieldDefinition.IsNumeric)
                    {
                        string identifierName = $"{identifierPrefix}.{DatasetTypeGenerator.GenerateIdentifier(fieldName)}";

                        decimal sum = tableLoadResult.Rows.SelectMany(m => m.Fields.Where(f => f.Key == fieldName)).Sum(s => s.Value != null ? Convert.ToDecimal(s.Value) : 0);
                        decimal average = tableLoadResult.Rows.SelectMany(m => m.Fields.Where(f => f.Key == fieldName)).Average(s => s.Value != null ? Convert.ToDecimal(s.Value) : 0);
                        decimal min = tableLoadResult.Rows.SelectMany(m => m.Fields.Where(f => f.Key == fieldName)).Min(s => s.Value != null ? Convert.ToDecimal(s.Value) : 0);
                        decimal max = tableLoadResult.Rows.SelectMany(m => m.Fields.Where(f => f.Key == fieldName)).Max(s => s.Value != null ? Convert.ToDecimal(s.Value) : 0);

                        IList<AggregatedField> aggregatedFields = new List<AggregatedField>
                        {
                            new AggregatedField
                            {
                                FieldDefinitionName = identifierName,
                                FieldType = AggregatedTypes.Sum,
                                Value = sum
                            },

                            new AggregatedField
                            {
                                FieldDefinitionName = identifierName,
                                FieldType = AggregatedTypes.Average,
                                Value = average
                            },

                            new AggregatedField
                            {
                                FieldDefinitionName = identifierName,
                                FieldType = AggregatedTypes.Min,
                                Value = min
                            },

                            new AggregatedField
                            {
                                FieldDefinitionName = identifierName,
                                FieldType = AggregatedType.Max,
                                Value = max
                            }
                        };

                        datasetAggregations.Fields = datasetAggregations.Fields.Concat(aggregatedFields);

                    }
                }
            }

            return datasetAggregations;
        }

        private async Task<BuildProject> ProcessDataset(Dataset dataset, 
            SpecificationSummary specification, 
            string relationshipId, 
            int version, 
            Reference user, 
            bool forceRefreshScopedProviders, 
            string correlationId)
        {
            string dataDefinitionId = dataset.Definition.Id;

            DatasetVersion datasetVersion = dataset.History.Where(v => v.Version == version).SingleOrDefault();
            if (datasetVersion == null)
            {
                _logger.Error("Dataset version not found for dataset '{name}' ({id}) version '{version}'", dataset.Id, dataset.Name, version);
                throw new NonRetriableException($"Dataset version not found for dataset '{dataset.Name}' ({dataset.Name}) version '{version}'");
            }

            string fullBlobName = datasetVersion.BlobName;

            DatasetDefinition datasetDefinition =
                    (await _datasetRepository.GetDatasetDefinitionsByQuery(m => m.Id == dataDefinitionId))?.FirstOrDefault();

            if (datasetDefinition == null)
            {
                _logger.Error($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");

                throw new NonRetriableException($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");
            }

            BuildProject buildProject = await _calcsRepository.GetBuildProjectBySpecificationId(specification.Id);

            if (buildProject == null)
            {
                _logger.Error($"Unable to find a build project for specification id: {specification.Id}");

                throw new NonRetriableException($"Unable to find a build project for id: {specification.Id}");
            }

            TableLoadResult loadResult = await GetTableResult(fullBlobName, datasetDefinition);

            if (loadResult == null)
            {
                _logger.Error($"Failed to load table result");

                throw new NonRetriableException($"Failed to load table result");
            }

            await PersistDataset(loadResult, 
                dataset, 
                datasetDefinition, 
                buildProject, 
                specification, 
                relationshipId, 
                version, 
                user, 
                forceRefreshScopedProviders, 
                correlationId);

            return buildProject;
        }

        private async Task<TableLoadResult> GetTableResult(string fullBlobName, DatasetDefinition datasetDefinition)
        {
            string dataset_cache_key = $"{CacheKeys.DatasetRows}:{datasetDefinition.Id}:{GetBlobNameCacheKey(fullBlobName)}".ToLowerInvariant();

            IEnumerable<TableLoadResult> tableLoadResults = await _cacheProvider.GetAsync<TableLoadResult[]>(dataset_cache_key);

            if (tableLoadResults.IsNullOrEmpty())
            {
                ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);

                if (blob == null)
                {
                    _logger.Error($"Failed to find blob with path: {fullBlobName}");
                    throw new NonRetriableException($"Failed to find blob with path: {fullBlobName}");
                }

                using (Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob))
                {
                    if (datasetStream == null || datasetStream.Length == 0)
                    {
                        _logger.Error($"Invalid blob returned: {fullBlobName}");
                        throw new NonRetriableException($"Invalid blob returned: {fullBlobName}");
                    }

                    tableLoadResults = _excelDatasetReader.Read(datasetStream, datasetDefinition).ToList();
                }

                await _cacheProvider.SetAsync(dataset_cache_key, tableLoadResults.ToArraySafe(), TimeSpan.FromDays(7), true);
            }

            return tableLoadResults.FirstOrDefault();
        }

        private async Task PersistDataset(TableLoadResult loadResult, 
            Dataset dataset, 
            DatasetDefinition datasetDefinition, 
            BuildProject buildProject, 
            SpecificationSummary specification, 
            string relationshipId, 
            int version, 
            Reference user, 
            bool forceRefreshScopedProviders, 
            string correlationId)
        {
            Guard.IsNullOrWhiteSpace(relationshipId, nameof(relationshipId));

            IList<ProviderSourceDataset> providerSourceDatasets = new List<ProviderSourceDataset>();

            if (buildProject.DatasetRelationships == null)
            {
                _logger.Error($"No dataset relationships found for build project with id : '{buildProject.Id}' for specification '{specification.Id}'");
                return;
            }

            DatasetRelationshipSummary relationshipSummary = buildProject.DatasetRelationships.FirstOrDefault(m => m.Relationship.Id == relationshipId);

            if (relationshipSummary == null)
            {
                _logger.Error($"No dataset relationship found for build project with id : {buildProject.Id} with data definition id {datasetDefinition.Id} and relationshipId '{relationshipId}'");
                return;
            }

            ConcurrentDictionary<string, ProviderSourceDataset> existingCurrent = new ConcurrentDictionary<string, ProviderSourceDataset>();

            IEnumerable<ProviderSourceDataset> existingCurrentDatasets = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.GetCurrentProviderSourceDatasets(specification.Id, relationshipId));

            if (existingCurrentDatasets.AnyWithNullCheck())
            {
                foreach (ProviderSourceDataset currentDataset in existingCurrentDatasets)
                {
                    existingCurrent.TryAdd(currentDataset.ProviderId, currentDataset);
                }
            }

            ConcurrentDictionary<string, ProviderSourceDataset> resultsByProviderId = new ConcurrentDictionary<string, ProviderSourceDataset>();

            ConcurrentDictionary<string, ProviderSourceDataset> updateCurrentDatasets = new ConcurrentDictionary<string, ProviderSourceDataset>();

            ApiResponse<Common.ApiClient.Providers.Models.ProviderVersion> providerApiSummaries = await _providersApiClientPolicy.ExecuteAsync(() =>
                _providersApiClient.GetProvidersByVersion(specification.ProviderVersionId));

            if (providerApiSummaries?.Content == null)
            {
                return;
            }

            IEnumerable<ProviderSummary> providerSummaries = _mapper.Map<IEnumerable<ProviderSummary>>(providerApiSummaries.Content.Providers);

            Parallel.ForEach(loadResult.Rows, (RowLoadResult row) =>
            {
                IEnumerable<string> allProviderIds = GetProviderIdsForIdentifier(datasetDefinition, row, providerSummaries);

                foreach (string providerId in allProviderIds)
                {
                    if (string.IsNullOrWhiteSpace(providerId))
                    {
                        _logger.Warning("Empty provider ID (missing provider) for dataset mapping row with identifier {}");
                        continue;
                    }

                    if (!resultsByProviderId.TryGetValue(providerId, out ProviderSourceDataset sourceDataset))
                    {
                        sourceDataset = new ProviderSourceDataset
                        {
                            DataGranularity = relationshipSummary.DataGranularity,
                            SpecificationId = specification.Id,
                            DefinesScope = relationshipSummary.DefinesScope,
                            DataRelationship = new Reference(relationshipSummary.Relationship.Id, relationshipSummary.Relationship.Name),
                            DatasetRelationshipSummary = new Reference(relationshipSummary.Id, relationshipSummary.Name),
                            ProviderId = providerId
                        };

                        sourceDataset.Current = new ProviderSourceDatasetVersion
                        {
                            Rows = new List<Dictionary<string, object>>(),
                            Dataset = new VersionReference(dataset.Id, dataset.Name, version),
                            Date = DateTimeOffset.Now.ToLocalTime(),
                            ProviderId = providerId,
                            Version = 1,
                            PublishStatus = Models.Versioning.PublishStatus.Draft,
                            ProviderSourceDatasetId = sourceDataset.Id,
                            Author = user
                        };

                        if (!resultsByProviderId.TryAdd(providerId, sourceDataset))
                        {
                            resultsByProviderId.TryGetValue(providerId, out sourceDataset);
                        }
                    }

                    if (_featureToggle.IsUseFieldDefinitionIdsInSourceDatasetsEnabled())
                    {
                        sourceDataset.DataDefinitionId = relationshipSummary.DatasetDefinition.Id;

                        Dictionary<string, object> rows = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, object> rowField in row.Fields)
                        {
                            foreach (TableDefinition tableDefinition in datasetDefinition.TableDefinitions)
                            {
                                FieldDefinition fieldDefinition = tableDefinition.FieldDefinitions.FirstOrDefault(m => m.Name == rowField.Key);
                                if (fieldDefinition != null)
                                {
                                    rows.Add(fieldDefinition.Id, rowField.Value);
                                }
                            }
                        }

                        sourceDataset.Current.Rows.Add(rows);
                    }
                    else
                    {
                        sourceDataset.DataDefinition = new Reference(relationshipSummary.DatasetDefinition.Id, relationshipSummary.DatasetDefinition.Name);

                        sourceDataset.Current.Rows.Add(row.Fields);
                    }
                }
            });

            ConcurrentBag<ProviderSourceDatasetVersion> historyToSave = new ConcurrentBag<ProviderSourceDatasetVersion>();

            List<Task> historySaveTasks = new List<Task>(resultsByProviderId.Count);

            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (KeyValuePair<string, ProviderSourceDataset> providerSourceDataset in resultsByProviderId)
            {
                await throttler.WaitAsync();
                historySaveTasks.Add(
                    Task.Run(async () =>
                    {
                        try
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
                                    newVersion = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                                            _sourceDatasetsVersionRepository.CreateVersion(newVersion, existingCurrent[providerId].Current, providerId));

                                    newVersion.Author = user;
                                    newVersion.Rows = sourceDataset.Current.Rows;

                                    sourceDataset.Current = newVersion;

                                    updateCurrentDatasets.TryAdd(providerId, sourceDataset);

                                    historyToSave.Add(newVersion);
                                }

                                existingCurrent.TryRemove(providerId, out ProviderSourceDataset existingProviderSourceDataset);
                            }
                            else
                            {
                                newVersion = sourceDataset.Current;

                                updateCurrentDatasets.TryAdd(providerId, sourceDataset);

                                historyToSave.Add(newVersion);
                            }
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(historySaveTasks.ToArray());

            if (updateCurrentDatasets.Count > 0)
            {
                _logger.Information($"Saving {updateCurrentDatasets.Count()} updated source datasets");

                await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.UpdateCurrentProviderSourceDatasets(updateCurrentDatasets.Values));
            }

            if (_featureToggle.IsProviderResultsSpecificationCleanupEnabled() && existingCurrent.Any())
            {
                _logger.Information($"Removing {existingCurrent.Count()} missing source datasets");

                await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.DeleteCurrentProviderSourceDatasets(existingCurrent.Values));

                foreach (IEnumerable<ProviderSourceDataset> providerSourceDataSets in existingCurrent.Values.Partition<ProviderSourceDataset>(1000))
                {
                    await SendProviderSourceDatasetCleanupMessageToTopic(specification.Id, ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup, providerSourceDataSets);
                }
            }

            if (historyToSave.Any())
            {
                _logger.Information($"Saving {historyToSave.Count()} items to history");

                await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                        _sourceDatasetsVersionRepository.SaveVersions(historyToSave));
            }

            Reference relationshipReference = new Reference(relationshipSummary.Relationship.Id, relationshipSummary.Relationship.Name);

            DatasetAggregations datasetAggregations = GenerateAggregations(datasetDefinition, loadResult, specification.Id, relationshipReference);

            if (!datasetAggregations.Fields.IsNullOrEmpty())
            {
                await _datasetsAggregationsRepository.CreateDatasetAggregations(datasetAggregations);
            }

            await _cacheProvider.RemoveAsync<List<CalculationAggregation>>($"{CacheKeys.DatasetAggregationsForSpecification}{specification.Id}");

            // need to remove all calculation result batches so that all calcs are created on calc run
            await _cacheProvider.RemoveByPatternAsync($"{CacheKeys.CalculationResults}{specification.Id}");

            bool isServiceBusService = _messengerService.GetType().GetInterfaces().Contains(typeof(IServiceBusService));

            if (isServiceBusService)
            {
                await ((IServiceBusService)_messengerService).CreateSubscription(ServiceBusConstants.TopicNames.JobNotifications, correlationId);
            }

            try
            {
                ApiResponse<bool> refreshCacheFromApi = await _providersApiClientPolicy.ExecuteAsync(() =>
                                 _providersApiClient.RegenerateProviderSummariesForSpecification(specification.Id, forceRefreshScopedProviders));

                if (!refreshCacheFromApi.StatusCode.IsSuccess() || refreshCacheFromApi?.Content == null)
                {
                    string errorMessage = $"Unable to re-generate providers while updating dataset '{relationshipId}' for specification '{specification.Id}' with status code: {refreshCacheFromApi.StatusCode}";

                    _logger.Information(errorMessage);

                    throw new RetriableException(errorMessage);
                }

                // if the scoped providers are being re-generated then wait for the job to finish
                if (Convert.ToBoolean(refreshCacheFromApi?.Content))
                {
                    bool jobCompletedSuccessfully;

                    if (isServiceBusService)
                    {
                        JobNotification scopedJob = await _messengerService.ReceiveMessage<JobNotification>($"{ServiceBusConstants.TopicNames.JobNotifications}/Subscriptions/{correlationId}", _ =>
                        {
                            return _?.JobType == DefinitionNames.PopulateScopedProvidersJob &&
                            _.SpecificationId == specification.Id &&
                            (_.CompletionStatus == CompletionStatus.Succeeded || _.CompletionStatus == CompletionStatus.Failed);
                        },
                        TimeSpan.FromMinutes(10));
                        jobCompletedSuccessfully = scopedJob?.CompletionStatus == CompletionStatus.Succeeded;
                    }
                    else
                    {
                        jobCompletedSuccessfully = await _jobManagement.WaitForJobsToComplete(new[] { DefinitionNames.PopulateScopedProvidersJob }, specification.Id);
                    }

                    if (!jobCompletedSuccessfully)
                    {
                        string errorMessage = $"Unable to re-generate providers while updating dataset '{relationshipId}' for specification '{specification.Id}' job didn't complete successfully in time";

                        _logger.Information(errorMessage);

                        throw new RetriableException(errorMessage);
                    }
                }
            }
            finally
            {
                if (isServiceBusService)
                {
                    await ((IServiceBusService)_messengerService).DeleteSubscription(ServiceBusConstants.TopicNames.JobNotifications, correlationId);
                }
            }
        }

        private static IEnumerable<string> GetProviderIdsForIdentifier(DatasetDefinition datasetDefinition, RowLoadResult row, IEnumerable<ProviderSummary> providerSummaries)
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
                            Dictionary<string, List<string>> lookup = GetDictionaryForIdentifierType(field.IdentifierFieldType, identifier, providerSummaries);
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
        private static Dictionary<string, List<string>> GetDictionaryForIdentifierType(IdentifierFieldType? identifierFieldType, string fieldIdentifierValue, IEnumerable<ProviderSummary> providerSummaries)
        {
            if (!identifierFieldType.HasValue)
            {
                return new Dictionary<string, List<string>>();
            }

            // Expression to filter ProviderSummaries - this selects which field on the ProviderSummary to filter on, eg UPIN
            Func<ProviderSummary, string> identifierSelectorExpression = GetIdentifierSelectorExpression(identifierFieldType.Value);

            // Find ProviderIds from the list of all providers - given the field and value of the ID
            IEnumerable<string> filteredIdentifiers = providerSummaries.Where(x => identifierSelectorExpression(x) == fieldIdentifierValue).Select(m => m.Id);

            return new Dictionary<string, List<string>> { { fieldIdentifierValue, filteredIdentifiers.ToList() } };
        }

        private static Func<ProviderSummary, string> GetIdentifierSelectorExpression(IdentifierFieldType identifierFieldType)
        {
            if (identifierFieldType == IdentifierFieldType.URN)
            {
                return x => x.URN;
            }
            else if (identifierFieldType == IdentifierFieldType.LACode)
            {
                return x => x.LACode;
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

        public static string GetBlobNameCacheKey(string blobPath)
        {
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(blobPath.ToLowerInvariant());
            return Convert.ToBase64String(plainTextBytes);
        }

        private async Task SendInstructAllocationsToJobService(string providerCacheKey, string specificationId, string userId, string userName, Trigger trigger, string correlationId, bool generateCalculationAggregations)
        {
            JobCreateModel job = new JobCreateModel
            {
                InvokerUserDisplayName = userName,
                InvokerUserId = userId,
                JobDefinitionId = generateCalculationAggregations ? JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob : JobConstants.DefinitionNames.CreateInstructAllocationJob,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>
                {
                    { "specification-id", specificationId },
                    { "provider-cache-key", providerCacheKey }
                },
                Trigger = trigger,
                CorrelationId = correlationId
            };

            Job createdJob = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(job));
            _logger.Information($"New job of type '{createdJob.JobDefinitionId}' created with id: '{createdJob.Id}'");
            return;
        }

        private async Task SendProviderSourceDatasetCleanupMessageToTopic(string specificationId, string topicName, IEnumerable<ProviderSourceDataset> providers)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(providers, nameof(providers));

            SpecificationProviders specificationProviders = new SpecificationProviders { SpecificationId = specificationId, Providers = providers.Select(x => x.ProviderId) };

            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "specificationId", specificationId },
                { "sfa-correlationId", Guid.NewGuid().ToString() }
            };

            await _messengerService.SendToTopic(topicName, specificationProviders, properties, true);
        }
    }
}
