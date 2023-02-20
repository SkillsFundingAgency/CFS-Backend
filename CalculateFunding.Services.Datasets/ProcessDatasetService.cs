using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Processing;
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
    public class ProcessDatasetService : JobProcessingService, IProcessDatasetService, IHealthChecker
    {
        private readonly IDatasetRepository _datasetRepository;
        private readonly IVersionRepository<DatasetVersion> _datasetVersionRepository;
        private readonly IExcelDatasetReader _excelDatasetReader;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICalcsRepository _calcsRepository;
        private readonly IBlobClient _blobClient;
        private readonly IMessengerService _messengerService;
        private readonly IJobManagement _jobManagement;
        private readonly IProviderSourceDatasetsRepository _providersResultsRepository;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly ISpecificationsApiClient _specsApiClient;
        private readonly IVersionBulkRepository<ProviderSourceDatasetVersion> _bulkSourceDatasetsVersionRepository;
        private readonly IProviderSourceDatasetBulkRepository _bulkProviderSourceDatasetRepository;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _providerResultsRepositoryPolicy;
        private readonly IDatasetsAggregationsRepository _datasetsAggregationsRepository;
        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly IFeatureToggle _featureToggle;
        private readonly IMapper _mapper;
        private readonly IProviderSourceDatasetVersionKeyProvider _datasetVersionKeyProvider;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;

        public ProcessDatasetService(
            IDatasetRepository datasetRepository,
            IVersionRepository<DatasetVersion> datasetVersionRepository,
            IExcelDatasetReader excelDatasetReader,
            ICacheProvider cacheProvider,
            ICalcsRepository calcsRepository,
            IBlobClient blobClient,
            IMessengerService messengerService,
            IProviderSourceDatasetsRepository providersResultsRepository,
            IProvidersApiClient providersApiClient,
            ISpecificationsApiClient specificationsApiClient,
            ILogger logger,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IDatasetsAggregationsRepository datasetsAggregationsRepository,
            IFeatureToggle featureToggle,
            IMapper mapper,
            IJobManagement jobManagement,
            IProviderSourceDatasetVersionKeyProvider datasetVersionKeyProvider,
            IVersionBulkRepository<ProviderSourceDatasetVersion> bulkSourceDatasetsVersionRepository,
            IProviderSourceDatasetBulkRepository bulkProviderSourceDatasetRepository) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(datasetVersionRepository, nameof(datasetVersionRepository));
            Guard.ArgumentNotNull(excelDatasetReader, nameof(excelDatasetReader));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(providersResultsRepository, nameof(providersResultsRepository));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetsAggregationsRepository, nameof(datasetsAggregationsRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.ProviderResultsRepository, nameof(datasetsResiliencePolicies.ProviderResultsRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.ProvidersApiClient, nameof(datasetsResiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(datasetVersionKeyProvider, nameof(datasetVersionKeyProvider));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.JobsApiClient, nameof(datasetsResiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(bulkSourceDatasetsVersionRepository, nameof(bulkSourceDatasetsVersionRepository));
            Guard.ArgumentNotNull(bulkProviderSourceDatasetRepository, nameof(bulkProviderSourceDatasetRepository));

            _datasetRepository = datasetRepository;
            _datasetVersionRepository = datasetVersionRepository;
            _excelDatasetReader = excelDatasetReader;
            _cacheProvider = cacheProvider;
            _calcsRepository = calcsRepository;
            _blobClient = blobClient;
            _providersResultsRepository = providersResultsRepository;
            _providersApiClient = providersApiClient;
            _specsApiClient = specificationsApiClient;
            _logger = logger;
            _providerResultsRepositoryPolicy = datasetsResiliencePolicies.ProviderResultsRepository;
            _datasetsAggregationsRepository = datasetsAggregationsRepository;
            _providersApiClientPolicy = datasetsResiliencePolicies.ProvidersApiClient;
            _featureToggle = featureToggle;
            _messengerService = messengerService;
            _mapper = mapper;
            _jobManagement = jobManagement;
            _datasetVersionKeyProvider = datasetVersionKeyProvider;
            _bulkSourceDatasetsVersionRepository = bulkSourceDatasetsVersionRepository;
            _bulkProviderSourceDatasetRepository = bulkProviderSourceDatasetRepository;

            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
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

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Dataset dataset = message.GetPayloadAsInstanceOf<Dataset>();

            // if we need to spawn any jobs then the map scoped dataset job needs to be set as the parent job
            string parentJobId = message.UserProperties.ContainsKey("parentJobId") ? message.UserProperties["parentJobId"].ToString() : null;
            bool isScopedJob = false;

            if (message.UserProperties.ContainsKey("isScopedJob") && message.UserProperties["isScopedJob"] != null)
            {
                isScopedJob = bool.Parse(message.UserProperties["isScopedJob"].ToString());
            }

            // if this is a scoped job then we don't want to trigger an instruct
            bool queueCalculationJob = !isScopedJob;

            if (message.UserProperties.ContainsKey("disableQueueCalculationJob") && message.UserProperties["disableQueueCalculationJob"] != null)
            {
                queueCalculationJob = !bool.Parse(message.UserProperties["disableQueueCalculationJob"].ToString());
            }

            if (dataset == null)
            {
                _logger.Error("A null dataset was provided to ProcessData");
                throw new NonRetriableException("A null dataset was provided to ProcessData");
            }

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("Specification Id key is missing in ProcessDataset message properties");
                throw new NonRetriableException("Specification Id key is missing in ProcessDataset message properties");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("A null or empty specification id was provided to ProcessData");
                throw new NonRetriableException("A null or empty specification id was provided to ProcessData");
            }

            if (!message.UserProperties.ContainsKey("relationship-id"))
            {
                _logger.Error("Relationship Id key is missing in ProcessDataset message properties");
                throw new NonRetriableException("Relationship Id key is missing in ProcessDataset message properties");
            }

            string relationshipId = message.UserProperties["relationship-id"].ToString();
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                _logger.Error("A null or empty relationship id was provided to ProcessDataset");
                throw new NonRetriableException("A null or empty relationship id was provided to ProcessDataset");
            }

            ApiResponse<SpecificationSummary> specificationSummaryResponse = await _specsApiClient.GetSpecificationSummaryById(specificationId);
            if (specificationSummaryResponse == null)
            {
                _logger.Error("Specification summary response was null");
                throw new NonRetriableException("Specification summary response was null");
            }

            if (specificationSummaryResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.Error($"Specification summary returned invalid status code of '{specificationSummaryResponse.StatusCode}'");
                throw new NonRetriableException($"Specification summary returned invalid status code of '{specificationSummaryResponse.StatusCode}'");
            }

            SpecificationSummary specification = specificationSummaryResponse.Content;

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (relationship == null)
            {
                _logger.Error($"Relationship not found for relationship id: {relationshipId}");
                throw new NonRetriableException($"Relationship not found for relationship id: {relationshipId}");
            }

            Reference user = message.GetUserDetails();

            string correlationId = message.GetCorrelationId();

            try
            {
                BuildProject buildProject = await ProcessDataset(dataset, specification, relationshipId, relationship.Current.DatasetVersion.Version, user, relationship.Current.IsSetAsProviderData, correlationId);

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

                        if (queueCalculationJob)
                        {
                            Trigger trigger = new Trigger
                            {
                                EntityId = relationshipId,
                                EntityType = nameof(DefinitionSpecificationRelationship),
                                Message = $"Processed dataset relationship: '{relationshipId}' for specification: '{specificationId}'"
                            };

                            IEnumerable<CalculationResponseModel> allCalculations = await _calcsRepository.GetCurrentCalculationsBySpecificationId(specificationId);

                            bool generateCalculationAggregations = !allCalculations.IsNullOrEmpty() &&
                                                                   SourceCodeHelpers.HasCalculationAggregateFunctionParameters(allCalculations.Select(m => m.SourceCode));

                            await SendInstructAllocationsToJobService($"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}",
                                $"{CacheKeys.SpecificationSummaryById}{specificationId}",
                                specificationId,
                                userId,
                                userName,
                                trigger,
                                correlationId,
                                generateCalculationAggregations);
                        }
                    }
                    catch (NonRetriableException argEx)
                    {
                        _logger.Error(argEx, $"Failed to create job of type '{DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'");
                        throw new NonRetriableException($"Failed to create job of type '{DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'", argEx);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to create job of type '{DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'");

                        throw new Exception($"Failed to create job of type '{DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'", ex);
                    }
                }
                
                //queue reindex spec job here also once we're done (NB I'm not sure I understand why the
                //use of Polly policies is so inconsistent in this service method
                await _specsApiClient.ReIndexSpecification(specificationId);

                if (isScopedJob && specification.ProviderSource != ProviderSource.FDZ)
                {
                    IEnumerable<DefinitionSpecificationRelationship> relationships = await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(c => c.Content.Current.Specification.Id == specificationId);

                    Dictionary<string, Dataset> nonScopeddatasets = new Dictionary<string, Dataset>();

                    foreach (DefinitionSpecificationRelationship nonScopedRelationship in relationships.Where(x => x.Id != relationshipId))
                    {
                        if (nonScopedRelationship == null || nonScopedRelationship.Current.DatasetVersion == null || string.IsNullOrWhiteSpace(nonScopedRelationship.Current.DatasetVersion.Id))
                        {
                            continue;
                        }

                        if (!nonScopeddatasets.TryGetValue(nonScopedRelationship.Current.DatasetVersion.Id, out Dataset nonScopedDataset))
                        {
                            nonScopedDataset = await _datasetRepository.GetDatasetByDatasetId(nonScopedRelationship.Current.DatasetVersion.Id);
                            nonScopeddatasets.Add(nonScopedRelationship.Current.DatasetVersion.Id, nonScopedDataset);
                        }

                        Trigger trigger = new Trigger
                        {
                            EntityId = nonScopedDataset.Id,
                            EntityType = nameof(Dataset),
                            Message = $"Mapping dataset: '{nonScopedDataset.Id}'"
                        };

                        JobCreateModel mapNonScopedDatasetJob = new JobCreateModel
                        {
                            InvokerUserDisplayName = user?.Name,
                            InvokerUserId = user?.Id,
                            JobDefinitionId = DefinitionNames.MapDatasetJob,
                            MessageBody = JsonConvert.SerializeObject(nonScopedDataset),
                            Properties = new Dictionary<string, string>
                                            {
                                                { "specification-id", nonScopedRelationship.Current.Specification.Id },
                                                { "relationship-id", nonScopedRelationship.Id },
                                                { "user-id", user?.Id },
                                                { "user-name", user?.Name },
                                                { "disableQueueCalculationJob", "true" }
                                            },
                            SpecificationId = nonScopedRelationship.Current.Specification.Id,
                            ParentJobId = parentJobId,
                            Trigger = trigger,
                            CorrelationId = correlationId,
                        };

                        await _jobManagement.QueueJob(mapNonScopedDatasetJob);
                    }
                }

                ItemsProcessed = 100;
                Outcome = "Processed Dataset";
            }
            catch (Exception exception)
            {
                // Unknown exception occurred so allow it to be retried
                _logger.Error(exception, $"Failed to run ProcessDataset with exception: {exception.Message} for relationship ID '{relationshipId}'");
                throw;
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

        public async Task MapFdzDatasets(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            AutoComplete = false;
            
            string specificationId = message.GetUserProperty<string>("specification-id");
            string relationshipId = message.GetUserProperty<string>("relationship-id");

            try
            { 
                Reference user = message.GetUserDetails();

                string correlationId = message.GetCorrelationId();

                if (string.IsNullOrWhiteSpace(specificationId))
                {
                    _logger.Error("Specification Id key is missing in MapFdzDatasets message properties");
                    throw new NonRetriableException("Failed to Process - specification id not provided");
                }

                IEnumerable<DefinitionSpecificationRelationship> relationships;

                if (string.IsNullOrWhiteSpace(relationshipId))
                {
                    relationships = await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(r
                    => r.Content.Current.Specification.Id == specificationId);
                }
                else
                {
                    relationships = new[] { await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId) };
                }

                Dictionary<string, Dataset> datasets = new Dictionary<string, Dataset>();

                IEnumerable<DefinitionSpecificationRelationship> mappedRelationships = relationships.Where(_ => _.Current.DatasetVersion != null);

                if (mappedRelationships.IsNullOrEmpty())
                {
                    // if there are no mapped relationships then we need to make sure the job doesn't stay InProgress
                    AutoComplete = true;
                    return;
                }

                foreach (DefinitionSpecificationRelationship relationship in mappedRelationships)
                {
                    if (!datasets.TryGetValue(relationship.Current.DatasetVersion.Id, out Dataset dataset))
                    {
                        dataset = await _datasetRepository.GetDatasetByDatasetId(relationship.Current.DatasetVersion.Id);
                        datasets.Add(relationship.Current.DatasetVersion.Id, dataset);
                    }

                    Trigger trigger = new Trigger
                    {
                        EntityId = dataset.Id,
                        EntityType = nameof(Dataset),
                        Message = $"Mapping dataset: '{dataset.Id}'"
                    };

                    JobCreateModel mapDatasetJob = new JobCreateModel
                    {
                        InvokerUserDisplayName = user?.Name,
                        InvokerUserId = user?.Id,
                        JobDefinitionId = DefinitionNames.MapDatasetJob,
                        MessageBody = JsonConvert.SerializeObject(dataset),
                        Properties = new Dictionary<string, string>
                                            {
                                                { "specification-id", specificationId },
                                                { "relationship-id", relationship.Id },
                                                { "user-id", user?.Id},
                                                { "user-name", user?.Name},
                                                { "disableQueueCalculationJob", "true" }
                                            },
                        SpecificationId = specificationId,
                        Trigger = trigger,
                        ParentJobId = Job.Id,
                        CorrelationId = correlationId,
                    };

                    await _jobManagement.QueueJob(mapDatasetJob);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Failed to run MapFdzDatasets with exception: {exception.Message}, for specification id '{specificationId}'");
                throw;
            }
        }

        private DatasetAggregations GenerateAggregations(DatasetDefinition datasetDefinition, TableLoadResult tableLoadResult, string specificationId, Reference datasetRelationship)
        {
            DatasetAggregations datasetAggregations = new DatasetAggregations
            {
                SpecificationId = specificationId,
                DatasetRelationshipId = datasetRelationship.Id,
                Fields = new List<AggregatedField>()
            };

            string identifierPrefix = $"Datasets.{_typeIdentifierGenerator.GenerateIdentifier(datasetRelationship.Name)}";

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
                        string identifierName = $"{identifierPrefix}.{_typeIdentifierGenerator.GenerateIdentifier(fieldName)}";

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
            string dataDefinitionId = dataset.Definition?.Id;

            DatasetVersion datasetVersion = (await _datasetVersionRepository.GetVersions(dataset.Id))?.SingleOrDefault(v => v.Version == version);
            
            if (datasetVersion == null)
            {
                _logger.Error("Dataset version not found for dataset '{name}' ({id}) version '{version}'", dataset.Id, dataset.Name, version);
                throw new NonRetriableException($"Dataset version not found for dataset '{dataset.Name}' ({dataset.Name}) version '{version}'");
            }

            string fullBlobName = datasetVersion.BlobName;

            DatasetDefinition datasetDefinition = null;

            if (dataDefinitionId != null)
            {
                datasetDefinition = (await _datasetRepository.GetDatasetDefinitionsByQuery(m => m.Id == dataDefinitionId))?.FirstOrDefault();

                if (datasetDefinition == null)
                {
                    _logger.Error($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");

                    throw new NonRetriableException($"Unable to find a data definition for id: {dataDefinitionId}, for blob: {fullBlobName}");
                }
            }

            BuildProject buildProject = await _calcsRepository.GetBuildProjectBySpecificationId(specification.Id);

            if (buildProject == null)
            {
                _logger.Error($"Unable to find a build project for specification id: {specification.Id}");

                throw new NonRetriableException($"Unable to find a build project for id: {specification.Id}");
            }

            TableLoadResult loadResult = null;

            if (datasetDefinition != null)
            {
                loadResult = await GetTableResult(fullBlobName, datasetDefinition);

                if (loadResult == null)
                {
                    _logger.Error($"Failed to load table result");

                    throw new NonRetriableException($"Failed to load table result");
                }
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
                correlationId,
                fullBlobName);

            return buildProject;
        }

        private async Task<TableLoadResult> GetTableResult(string fullBlobName, DatasetDefinition datasetDefinition)
        {
            string datasetCacheKey = $"{CacheKeys.DatasetRows}:{datasetDefinition.Id}:{GetBlobNameCacheKey(fullBlobName)}".ToLowerInvariant();

            IEnumerable<TableLoadResult> tableLoadResults = await _cacheProvider.GetAsync<TableLoadResult[]>(datasetCacheKey);

            if (tableLoadResults.IsNullOrEmpty())
            {
                ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);

                if (blob == null)
                {
                    _logger.Error($"Failed to find blob with path: {fullBlobName}");
                    throw new NonRetriableException($"Failed to find blob with path: {fullBlobName}");
                }

                await using (Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob))
                {
                    if (datasetStream == null || datasetStream.Length == 0)
                    {
                        _logger.Error($"Invalid blob returned: {fullBlobName}");
                        throw new NonRetriableException($"Invalid blob returned: {fullBlobName}");
                    }

                    tableLoadResults = _excelDatasetReader.Read(datasetStream, datasetDefinition).ToList();
                }

                await _cacheProvider.SetAsync(datasetCacheKey, tableLoadResults.ToArraySafe(), TimeSpan.FromDays(7), true);
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
            string correlationId,
            string fullBlobName)
        {
            Guard.IsNullOrWhiteSpace(relationshipId, nameof(relationshipId));

            if (buildProject.DatasetRelationships == null)
            {
                _logger.Error($"No dataset relationships found for build project with id : '{buildProject.Id}' for specification '{specification.Id}'");
                return;
            }

            DatasetRelationshipSummary relationshipSummary = buildProject.DatasetRelationships.FirstOrDefault(m => m.Relationship.Id == relationshipId);

            if (relationshipSummary == null)
            {
                _logger.Error($"No dataset relationship found for build project with id : {buildProject.Id} with relationshipId '{relationshipId}'");
                return;
            }

            if (relationshipSummary.RelationshipType == DatasetRelationshipType.ReleasedData)
            {
                datasetDefinition = GetReleasedDataDefinition(relationshipSummary.PublishedSpecificationConfiguration);
                
                loadResult = await GetTableResult(fullBlobName, datasetDefinition);

                if (loadResult == null)
                {
                    _logger.Error($"Failed to load table result");

                    throw new NonRetriableException($"Failed to load table result");
                }
            }

            ConcurrentDictionary<string, DocumentEntity<ProviderSourceDataset>> existingCurrent = new ConcurrentDictionary<string, DocumentEntity<ProviderSourceDataset>>();

            IEnumerable<DocumentEntity<ProviderSourceDataset>> existingCurrentDatasets = await _providerResultsRepositoryPolicy.ExecuteAsync(() =>
                _providersResultsRepository.GetCurrentProviderSourceDatasets(specification.Id, relationshipId));

            if (existingCurrentDatasets.AnyWithNullCheck())
            {
                foreach (DocumentEntity<ProviderSourceDataset> currentDataset in existingCurrentDatasets)
                {
                    existingCurrent.TryAdd(currentDataset.Content.ProviderId, currentDataset);
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
                            ProviderId = providerId,
                            DatasetRelationshipType = relationshipSummary.RelationshipType
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

                    if (relationshipSummary.RelationshipType == DatasetRelationshipType.ReleasedData)
                    {
                        Dictionary<string, object> rows = new Dictionary<string, object>();

                        foreach (PublishedSpecificationItem fundingLine in relationshipSummary.PublishedSpecificationConfiguration.FundingLines)
                        {
                            string key = $"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_{fundingLine.TemplateId}_{fundingLine.Name}";

                            if (row.Fields.ContainsKey(key))
                            {
                                rows.Add(key, row.Fields[key]);
                            }
                            if (relationshipSummary.PublishedSpecificationConfiguration.IncludeCarryForward)
                            {
                                string keyCarryOver = $"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_{fundingLine.TemplateId}_{fundingLine.Name}_CarryOver";

                                if (row.Fields.ContainsKey(keyCarryOver))
                                {
                                    rows.Add(keyCarryOver, row.Fields[keyCarryOver]);
                                }
                            }
                        }

                        foreach (PublishedSpecificationItem calcTemplate in relationshipSummary.PublishedSpecificationConfiguration.Calculations)
                        {
                            string key = $"{CodeGenerationDatasetTypeConstants.CalculationPrefix}_{calcTemplate.TemplateId}_{calcTemplate.Name}";

                            if (row.Fields.ContainsKey(key))
                            {
                                rows.Add(key, row.Fields[key]);
                            }
                        }

                        sourceDataset.Current.Rows.Add(rows);
                        sourceDataset.DataDefinitionId = relationshipSummary.Id;
                    }
                    else
                    {
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
                }
            });

            ConcurrentBag<ProviderSourceDatasetVersion> historyToSave = new ConcurrentBag<ProviderSourceDatasetVersion>();
            
            Task[] createVersionTasks = new Task[resultsByProviderId.Count];

            for (int resultByProviderId = 0; resultByProviderId < resultsByProviderId.Count; resultByProviderId++)
            {
                KeyValuePair<string, ProviderSourceDataset> providerSourceDataset = resultsByProviderId.ElementAt(resultByProviderId);

                createVersionTasks[resultByProviderId] = Task.Run(async () =>
                {
                    string providerId = providerSourceDataset.Key;
                    ProviderSourceDataset sourceDataset = providerSourceDataset.Value;

                    if (existingCurrent.ContainsKey(providerId))
                    {
                        ProviderSourceDatasetVersion newVersion = (ProviderSourceDatasetVersion)existingCurrent[providerId].Content.Current.Clone();

                        string existingDatasetJson = JsonConvert.SerializeObject(existingCurrent[providerId].Content.Current.Rows);
                        string latestDatasetJson = JsonConvert.SerializeObject(sourceDataset.Current.Rows);

                        // if it's been deleted then the rows will match but if the code has reached here then we always want to undelete and create a new version
                        if (existingDatasetJson != latestDatasetJson || existingCurrent[providerId].Deleted)
                        {
                                newVersion = await _bulkSourceDatasetsVersionRepository.CreateVersion(newVersion,
                                existingCurrent[providerId].Content.Current,
                                providerId);
                                    
                            newVersion.Author = user;
                            newVersion.Rows = sourceDataset.Current.Rows;

                            sourceDataset.Current = newVersion;

                            updateCurrentDatasets.TryAdd(providerId, sourceDataset);

                            historyToSave.Add(newVersion);
                        }

                        existingCurrent.TryRemove(providerId, out DocumentEntity<ProviderSourceDataset> _);
                    }
                    else
                    {
                        updateCurrentDatasets.TryAdd(providerId, sourceDataset);

                        historyToSave.Add(sourceDataset.Current);
                    }
                });
            }

            await TaskHelper.WhenAllAndThrow(createVersionTasks);

            if (updateCurrentDatasets.Count > 0)
            {
                _logger.Information($"Saving {updateCurrentDatasets.Count()} updated source datasets");

                await _bulkProviderSourceDatasetRepository.UpdateCurrentProviderSourceDatasets(updateCurrentDatasets.Values);
            }

            // only need to delete source datasets which aren't already deleted
            IEnumerable<ProviderSourceDataset> cleanupDatasets = existingCurrent.Where(_ => _.Value.Deleted != true).Select(_ => _.Value.Content);

            if (_featureToggle.IsProviderResultsSpecificationCleanupEnabled() && cleanupDatasets.AnyWithNullCheck())
            {
                _logger.Information($"Removing {cleanupDatasets.Count()} missing source datasets");

                await _bulkProviderSourceDatasetRepository.DeleteCurrentProviderSourceDatasets(cleanupDatasets);

                foreach (IEnumerable<ProviderSourceDataset> providerSourceDataSets in cleanupDatasets.Partition(1000))
                {
                    await SendProviderSourceDatasetCleanupMessageToTopic(specification.Id, ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup, providerSourceDataSets);
                }
            }

            if (historyToSave.Any())
            {
                _logger.Information($"Saving {historyToSave.Count()} items to history");
                
                Task<HttpStatusCode>[] saveHistoryTasks = new Task<HttpStatusCode>[historyToSave.Count];

                for (int history = 0; history < historyToSave.Count; history++)
                {
                    int versionIndex = history;

                    saveHistoryTasks[history] = Task.Run(() => _bulkSourceDatasetsVersionRepository.SaveVersion(historyToSave.ElementAt(versionIndex)));
                }

                await TaskHelper.WhenAllAndThrow(saveHistoryTasks);
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

            // need to remove code context as the datasets have changed
            await _cacheProvider.RemoveByPatternAsync($"{CacheKeys.CodeContext}{specification.Id}");

            if (specification.ProviderSource != ProviderSource.FDZ)
            {
                bool jobCompletedSuccessfully = await _jobManagement.QueueJobAndWait(async () =>
                    {
                        ApiResponse<bool> refreshCacheFromApi = await _providersApiClientPolicy.ExecuteAsync(() =>
                            _providersApiClient.RegenerateProviderSummariesForSpecification(specification.Id, forceRefreshScopedProviders));

                        if (refreshCacheFromApi?.StatusCode.IsSuccess() == false)
                        {
                            string errorMessage = $"Unable to re-generate providers while updating dataset '{relationshipId}' for specification '{specification.Id}' with status code: {refreshCacheFromApi.StatusCode}";

                            _logger.Error(errorMessage);

                            throw new NonRetriableException(errorMessage);
                        }

                        // returns true if job queued
                        return refreshCacheFromApi.Content;
                    },
                    DefinitionNames.PopulateScopedProvidersJob,
                    specification.Id,
                    correlationId,
                    ServiceBusConstants.TopicNames.JobNotifications);

                if (!jobCompletedSuccessfully)
                {
                    string errorMessage = $"Unable to re-generate providers while updating dataset '{relationshipId}' for specification '{specification.Id}' job didn't complete successfully in time";

                    _logger.Information(errorMessage);

                    throw new RetriableException(errorMessage);
                }
            }
        }

        private static DatasetDefinition GetReleasedDataDefinition(PublishedSpecificationConfiguration configuration)
        {
            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = configuration.SpecificationId,
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition
                    {
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition
                            {
                                IdentifierFieldType = IdentifierFieldType.UKPRN,
                                Type = FieldType.Integer,
                                Name = "UKPRN",
                                Required = true
                            }
                        }
                    }
                }
            };

            foreach (PublishedSpecificationItem publishedSpecificationItem in configuration.FundingLines)
            {
                datasetDefinition.TableDefinitions.First().FieldDefinitions.Add(new FieldDefinition
                {
                    Name = $"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_{publishedSpecificationItem.TemplateId}_{publishedSpecificationItem.Name}",
                    Type = publishedSpecificationItem.FieldType
                });

                if (configuration.IncludeCarryForward)
                {
                    datasetDefinition.TableDefinitions.First().FieldDefinitions.Add(new FieldDefinition
                    {
                        Name = $"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_{publishedSpecificationItem.TemplateId}_{publishedSpecificationItem.Name}_CarryOver",
                        Type = publishedSpecificationItem.FieldType
                    });
                }
            }

            foreach (PublishedSpecificationItem publishedSpecificationItem in configuration.Calculations)
            {
                datasetDefinition.TableDefinitions.First().FieldDefinitions.Add(new FieldDefinition
                {
                    Name = $"{CodeGenerationDatasetTypeConstants.CalculationPrefix}_{publishedSpecificationItem.TemplateId}_{publishedSpecificationItem.Name}",
                    Type = publishedSpecificationItem.FieldType
                });
            }

            return datasetDefinition;
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

        private async Task SendInstructAllocationsToJobService(string providerCacheKey, string specificationSummaryCacheKey, string specificationId, string userId, string userName, Trigger trigger, string correlationId, bool generateCalculationAggregations)
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
                    { "provider-cache-key", providerCacheKey },
                    { "specification-summary-cache-key", specificationSummaryCacheKey }
                },
                Trigger = trigger,
                CorrelationId = correlationId
            };

            Job createdJob = await _jobManagement.QueueJob(job);
            
            _logger.Information($"New job of type '{createdJob.JobDefinitionId}' created with id: '{createdJob.Id}'");
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
