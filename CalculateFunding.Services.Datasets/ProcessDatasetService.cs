using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using Serilog;

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
        private readonly IProvidersResultsRepository _providersResultsRepository;
        private readonly IResultsRepository _resultsRepository;
        private readonly IProviderService _providerService;
        private readonly IVersionRepository<ProviderSourceDatasetVersion> _sourceDatasetsVersionRepository;
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly Policy _providerResultsRepositoryPolicy;
        private readonly IDatasetsAggregationsRepository _datasetsAggregationsRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly Policy _jobsApiClientPolicy;
        private readonly IJobsApiClient _jobsApiClient;

        public ProcessDatasetService(
            IDatasetRepository datasetRepository,
            IExcelDatasetReader excelDatasetReader,
            ICacheProvider cacheProvider,
            ICalcsRepository calcsRepository,
            IBlobClient blobClient,
            IMessengerService messengerService,
            IProvidersResultsRepository providersResultsRepository,
            IResultsRepository resultsRepository,
            IProviderService providerService,
            IVersionRepository<ProviderSourceDatasetVersion> sourceDatasetsVersionRepository,
            ILogger logger,
            ITelemetry telemetry,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IDatasetsAggregationsRepository datasetsAggregationsRepository,
            IFeatureToggle featureToggle,
            IJobsApiClient jobsApiClient)
        {
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(excelDatasetReader, nameof(excelDatasetReader));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(providersResultsRepository, nameof(providersResultsRepository));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(datasetsAggregationsRepository, nameof(datasetsAggregationsRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));

            _datasetRepository = datasetRepository;
            _excelDatasetReader = excelDatasetReader;
            _cacheProvider = cacheProvider;
            _calcsRepository = calcsRepository;
            _blobClient = blobClient;
            _providersResultsRepository = providersResultsRepository;
            _resultsRepository = resultsRepository;
            _providerService = providerService;
            _sourceDatasetsVersionRepository = sourceDatasetsVersionRepository;
            _logger = logger;
            _telemetry = telemetry;
            _providerResultsRepositoryPolicy = datasetsResiliencePolicies.ProviderResultsRepository;
            _datasetsAggregationsRepository = datasetsAggregationsRepository;
            _featureToggle = featureToggle;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = datasetsResiliencePolicies.JobsApiClient;
            _messengerService = messengerService;
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

            await UpdateJobStatus(jobId, null, 0);

            if (dataset == null)
            {
                _logger.Error("A null dataset was provided to ProcessData");
                await UpdateJobStatus(jobId, false, 100, "Failed to Process - null dataset provided");
                return;
            }

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("Specification Id key is missing in ProcessDataset message properties");
                await UpdateJobStatus(jobId, false, 100, "Failed to Process - specification id not provided");
                return;
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("A null or empty specification id was provided to ProcessData");
                await UpdateJobStatus(jobId, false, 100, "Failed to Process - specification if is null or empty");
                return;
            }

            if (!message.UserProperties.ContainsKey("relationship-id"))
            {
                _logger.Error("Relationship Id key is missing in ProcessDataset message properties");
                await UpdateJobStatus(jobId, false, 100, "Failed to Process - relationship id not provided");
                return;
            }

            string relationshipId = message.UserProperties["relationship-id"].ToString();
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                _logger.Error("A null or empty relationship id was provided to ProcessDataset");
                await UpdateJobStatus(jobId, false, 100, "Failed to Process - relationship id is null or empty");
                return;
            }

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (relationship == null)
            {
                _logger.Error($"Relationship not found for relationship id: {relationshipId}");
                await UpdateJobStatus(jobId, false, 100, "Failed to Process - relationship not found");
                return;
            }

            BuildProject buildProject = null;

            Reference user = message.GetUserDetails();

            try
            {
                buildProject = await ProcessDataset(dataset, specificationId, relationshipId, relationship.DatasetVersion.Version, user);

                await UpdateJobStatus(jobId, true, 100, "Processed Dataset");
            }
            catch (NonRetriableException argEx)
            {
                // This type of exception is not retriable so fail
                _logger.Error(argEx, $"Failed to run ProcessDataset with exception: {argEx.Message} for relationship ID '{relationshipId}'");
                await UpdateJobStatus(jobId, false, 100, $"Failed to run Process - {argEx.Message}");
                return;
            }
            catch (Exception exception)
            {
                // Unknown exception occurred so allow it to be retried
                _logger.Error(exception, $"Failed to run ProcessDataset with exception: {exception.Message} for relationship ID '{relationshipId}'");
                throw;
            }

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
                    string correlationId = message.GetCorrelationId();

                    IEnumerable<CalculationCurrentVersion> allCalculations = await _calcsRepository.GetCurrentCalculationsBySpecificationId(specificationId);

                    bool generateCalculationAggregations = allCalculations.IsNullOrEmpty() ? false : SourceCodeHelpers.HasCalculationAggregateFunctionParameters(allCalculations.Select(m => m.SourceCode));

                    await SendInstructAllocationsToJobService($"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}", specificationId, userId, userName, trigger, correlationId, generateCalculationAggregations);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'");

                    throw new Exception($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'", ex);
                }
            }
        }

        async public Task<IActionResult> GetDatasetAggregationsBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<DatasetAggregations> aggregates = await _datasetsAggregationsRepository.GetDatasetAggregationsForSpecificationId(specificationId);

            if (aggregates == null)
            {
                aggregates = Enumerable.Empty<DatasetAggregations>();
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
                                FieldType = AggregatedType.Sum,
                                Value = sum
                            },

                            new AggregatedField
                            {
                                FieldDefinitionName = identifierName,
                                FieldType = AggregatedType.Average,
                                Value = average
                            },

                            new AggregatedField
                            {
                                FieldDefinitionName = identifierName,
                                FieldType = AggregatedType.Min,
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

        private async Task<BuildProject> ProcessDataset(Dataset dataset, string specificationId, string relationshipId, int version, Reference user)
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

            BuildProject buildProject = await _calcsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error($"Unable to find a build project for specification id: {specificationId}");

                throw new NonRetriableException($"Unable to find a build project for id: {specificationId}");
            }

            TableLoadResult loadResult = await GetTableResult(fullBlobName, datasetDefinition);

            if (loadResult == null)
            {
                _logger.Error($"Failed to load table result");

                throw new NonRetriableException($"Failed to load table result");
            }

            await PersistDataset(loadResult, dataset, datasetDefinition, buildProject, specificationId, relationshipId, version, user);

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

        private async Task PersistDataset(TableLoadResult loadResult, Dataset dataset, DatasetDefinition datasetDefinition, BuildProject buildProject, string specificationId, string relationshipId, int version, Reference user)
        {
            IEnumerable<ProviderSummary> providerSummaries = await _providerService.FetchCoreProviderData();

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

            ConcurrentDictionary<string, ProviderSourceDataset> existingCurrent = new ConcurrentDictionary<string, ProviderSourceDataset>();

            IEnumerable<ProviderSourceDataset> existingCurrentDatasets = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.GetCurrentProviderSourceDatasets(specificationId, relationshipId));

            if (existingCurrentDatasets.AnyWithNullCheck())
            {
                foreach (ProviderSourceDataset currentDataset in existingCurrentDatasets)
                {
                    existingCurrent.TryAdd(currentDataset.ProviderId, currentDataset);
                }
            }

            ConcurrentDictionary<string, ProviderSourceDataset> resultsByProviderId = new ConcurrentDictionary<string, ProviderSourceDataset>();

            ConcurrentDictionary<string, ProviderSourceDataset> updateCurrentDatasets = new ConcurrentDictionary<string, ProviderSourceDataset>();

            Parallel.ForEach(loadResult.Rows, (RowLoadResult row) =>
            {
                IEnumerable<string> allProviderIds = GetProviderIdsForIdentifier(datasetDefinition, row, providerSummaries);

                foreach (string providerId in allProviderIds)
                {
                    if (!resultsByProviderId.TryGetValue(providerId, out ProviderSourceDataset sourceDataset))
                    {
                        sourceDataset = new ProviderSourceDataset
                        {
                            DataGranularity = relationshipSummary.DataGranularity,
                            SpecificationId = specificationId,
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
                                    newVersion = await _sourceDatasetsVersionRepository.CreateVersion(newVersion, existingCurrent[providerId].Current, providerId);
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

            if (existingCurrent.Any())
            {
                 _logger.Information($"Removing {existingCurrent.Count()} missing source datasets");

                await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.DeleteCurrentProviderSourceDatasets(existingCurrent.Values));

                foreach (IEnumerable<ProviderSourceDataset> providerSourceDataSets in existingCurrent.Values.Partition<ProviderSourceDataset>(1000))
                {
                    await SendProviderSourceDatasetCleanupMessageToTopic(specificationId, ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup, providerSourceDataSets);
                }
            }

            if (historyToSave.Any())
            {
                _logger.Information($"Saving {historyToSave.Count()} items to history");
                await _sourceDatasetsVersionRepository.SaveVersions(historyToSave);
            }

            Reference relationshipReference = new Reference(relationshipSummary.Relationship.Id, relationshipSummary.Relationship.Name);

            DatasetAggregations datasetAggregations = GenerateAggregations(datasetDefinition, loadResult, specificationId, relationshipReference);

            if (!datasetAggregations.Fields.IsNullOrEmpty())
            {
                await _datasetsAggregationsRepository.CreateDatasetAggregations(datasetAggregations);
            }

            await _cacheProvider.RemoveAsync<List<CalculationAggregation>>($"{CacheKeys.DatasetAggregationsForSpecification}{specificationId}");


            await PopulateProviderSummariesForSpecification(specificationId, providerSummaries);
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

        async Task PopulateProviderSummariesForSpecification(string specificationId, IEnumerable<ProviderSummary> allCachedProviders)
        {
            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            IEnumerable<string> providerIdsAll = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _resultsRepository.GetAllProviderIdsForSpecificationId(specificationId));

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
