using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using TemplateMetadataCalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionSpecificationRelationshipService :
        IDefinitionSpecificationRelationshipService, IHealthChecker
    {
        private readonly IDatasetRepository _datasetRepository;
        private readonly IVersionRepository<DatasetVersion> _versionDatasetRepository;
        private readonly ILogger _logger;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IValidator<CreateDefinitionSpecificationRelationshipModel> _createRelationshipModelValidator;
        private readonly IValidator<ValidateDefinitionSpecificationRelationshipModel> _validateRelationshipModelValidator;
        private readonly IValidator<UpdateDefinitionSpecificationRelationshipModel> _updateRelationshipModelValidator;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IMessengerService _messengerService;
        private readonly ICalcsRepository _calcsRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IJobManagement _jobManagement;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;
        private readonly Polly.AsyncPolicy _policiesApiClientPolicy;
        private readonly Polly.AsyncPolicy _relationshipRepositoryPolicy;
        private readonly AsyncPolicy _datasetRepositoryPolicy;
        private readonly IVersionRepository<DefinitionSpecificationRelationshipVersion> _relationshipVersionRepository;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;

        public DefinitionSpecificationRelationshipService(IDatasetRepository datasetRepository,
            IVersionRepository<DatasetVersion> versionDatasetRepository,
            ILogger logger,
            ISpecificationsApiClient specificationsApiClient,
            IValidator<CreateDefinitionSpecificationRelationshipModel> createRelationshipModelValidator,
            IMessengerService messengerService,
            ICalcsRepository calcsRepository,
            ICacheProvider cacheProvider,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IJobManagement jobManagement,
            IDateTimeProvider dateTimeProvider,
            IValidator<ValidateDefinitionSpecificationRelationshipModel> validateRelationshipModelValidator,
            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository,
            IPoliciesApiClient policiesApiClient,
            IValidator<UpdateDefinitionSpecificationRelationshipModel> updateRelationshipModelValidator)
        {
            Guard.ArgumentNotNull(dateTimeProvider, nameof(dateTimeProvider));
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(versionDatasetRepository, nameof(versionDatasetRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(createRelationshipModelValidator, nameof(createRelationshipModelValidator));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.SpecificationsApiClient, nameof(datasetsResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.PoliciesApiClient, nameof(datasetsResiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.DatasetRepository, nameof(datasetsResiliencePolicies.DatasetRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.RelationshipVersionRepository, nameof(datasetsResiliencePolicies.RelationshipVersionRepository));
            Guard.ArgumentNotNull(validateRelationshipModelValidator, nameof(validateRelationshipModelValidator));
            Guard.ArgumentNotNull(relationshipVersionRepository, nameof(relationshipVersionRepository));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(updateRelationshipModelValidator, nameof(updateRelationshipModelValidator));

            _datasetRepository = datasetRepository;
            _versionDatasetRepository = versionDatasetRepository;
            _logger = logger;
            _specificationsApiClient = specificationsApiClient;
            _createRelationshipModelValidator = createRelationshipModelValidator;
            _messengerService = messengerService;
            _calcsRepository = calcsRepository;
            _cacheProvider = cacheProvider;
            _jobManagement = jobManagement;
            _dateTimeProvider = dateTimeProvider;
            _specificationsApiClientPolicy = datasetsResiliencePolicies.SpecificationsApiClient;
            _policiesApiClientPolicy = datasetsResiliencePolicies.PoliciesApiClient;
            _datasetRepositoryPolicy = datasetsResiliencePolicies.DatasetRepository;
            _relationshipRepositoryPolicy = datasetsResiliencePolicies.RelationshipVersionRepository;
            _relationshipVersionRepository = relationshipVersionRepository;
            _validateRelationshipModelValidator = validateRelationshipModelValidator;
            _policiesApiClient = policiesApiClient;
            _updateRelationshipModelValidator = updateRelationshipModelValidator;

            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_datasetRepository).IsHealthOk();
            string queueName = ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification;
            (bool Ok, string Message) messengerServiceHealth = await _messengerService.IsHealthOk(queueName);
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(DatasetService)
            };
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth
            { HealthOk = messengerServiceHealth.Ok, DependencyName = $"{_messengerService.GetType().GetFriendlyName()} for queue: {queueName}", Message = messengerServiceHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }

        public async Task<IActionResult> CreateRelationship(CreateDefinitionSpecificationRelationshipModel model,
            Reference author,
            string correlationId)
        {
            if (model == null)
            {
                _logger.Error("Null CreateDefinitionSpecificationRelationshipModel was provided to CreateRelationship");
                return new BadRequestObjectResult("Null CreateDefinitionSpecificationRelationshipModel was provided");
            }

            BadRequestObjectResult validationResult = (await _createRelationshipModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            DatasetDefinition definition = null;

            if (model.RelationshipType == DatasetRelationshipType.Uploaded)
            {
                definition = await _datasetRepository.GetDatasetDefinition(model.DatasetDefinitionId);

                if (definition == null)
                {
                    _logger.Error($"Dataset definition was not found for id {model.DatasetDefinitionId}");
                    return new StatusCodeResult(412);
                }
            }

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(model.SpecificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                _logger.Error($"Specification was not found for id {model.SpecificationId}");
                return new StatusCodeResult(412);
            }

            SpecModel.SpecificationSummary specification = specificationApiResponse.Content;

            DefinitionSpecificationRelationship relationship = new DefinitionSpecificationRelationship()
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.Name
            };

            DefinitionSpecificationRelationshipVersion relationshipVersion = new DefinitionSpecificationRelationshipVersion
            {
                Name = model.Name,
                DatasetDefinition = definition != null ? new Reference(definition.Id, definition.Name) : null,
                Specification = new Reference(specification.Id, specification.Name),
                Description = model.Description,
                RelationshipId = relationship.Id,
                IsSetAsProviderData = model.IsSetAsProviderData,
                UsedInDataAggregations = model.UsedInDataAggregations,
                Author = author,
                LastUpdated = _dateTimeProvider.UtcNow,
                ConverterEnabled = model.ConverterEnabled,
                RelationshipType = model.RelationshipType
            };

            if (model.RelationshipType == DatasetRelationshipType.ReleasedData && !string.IsNullOrWhiteSpace(model.TargetSpecificationId))
            {
                relationshipVersion.PublishedSpecificationConfiguration = await CreatePublishedSpecificationConfiguration(model.TargetSpecificationId, model.FundingLineIds, model.CalculationIds);
            }

            relationship.Current = relationshipVersion = await _relationshipVersionRepository.CreateVersion(relationshipVersion);

            HttpStatusCode statusCode = await _datasetRepositoryPolicy.ExecuteAsync(() => _datasetRepository.SaveDefinitionSpecificationRelationship(relationship));

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save relationship with status code: {statusCode}");
                return new StatusCodeResult((int)statusCode);
            }

            await _relationshipVersionRepository.SaveVersion(relationshipVersion);

            IDictionary<string, string> properties = MessageExtensions.BuildMessageProperties(correlationId, author);

            await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification,
                new AssignDefinitionRelationshipMessage
                {
                    SpecificationId = specification.Id,
                    RelationshipId = relationship.Id
                },
                properties);

            if (relationshipVersion.RelationshipType == DatasetRelationshipType.ReleasedData)
            {
                await _jobManagement.QueueJob(new JobCreateModel
                {
                    InvokerUserDisplayName = author.Name,
                    InvokerUserId = author.Id,
                    JobDefinitionId = JobConstants.DefinitionNames.PublishDatasetsDataJob,
                    SpecificationId = specification.Id,
                    Trigger = new Trigger
                    {
                        EntityId = specification.Id,
                        EntityType = "Specification",
                        Message = "New Csv file generation triggered by dataset relationship spec"
                    },
                    Properties = new Dictionary<string, string> { { "specification-id", specification.Id },
                        { "relationship-id", relationshipVersion.RelationshipId } },
                    CorrelationId = correlationId
                });
            }

            DatasetRelationshipSummary relationshipSummary = new DatasetRelationshipSummary
            {
                Name = relationshipVersion.Name,
                Id = Guid.NewGuid().ToString(),
                Relationship = new Reference(relationshipVersion.Id, relationshipVersion.Name),
                DatasetDefinition = definition,
                DatasetDefinitionId = definition?.Id,
                DataGranularity = relationshipVersion.UsedInDataAggregations ? DataGranularity.MultipleRowsPerProvider : DataGranularity.SingleRowPerProvider,
                DefinesScope = relationshipVersion.IsSetAsProviderData
            };

            await _calcsRepository.UpdateBuildProjectRelationships(specification.Id, relationshipSummary);

            await _cacheProvider.RemoveAsync<IEnumerable<DatasetSchemaRelationshipModel>>($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{specification.Id}");

            return new OkObjectResult(relationshipVersion);
        }

        public async Task<IActionResult> UpdateRelationship(UpdateDefinitionSpecificationRelationshipModel model, string specificationId, string relationshipId)
        {
            Guard.IsNullOrWhiteSpace(relationshipId, nameof(relationshipId));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            if (model == null)
            {
                _logger.Error("Null UpdateDefinitionSpecificationRelationshipModel was provided to UpdateRelationship");
                return new BadRequestObjectResult("Null EditDefinitionSpecificationRelationshipModel was provided to UpdateRelationship");
            }

            model.RelationshipId = relationshipId;
            model.SpecificationId = specificationId;
            BadRequestObjectResult validationResult = (await _updateRelationshipModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            DefinitionSpecificationRelationship definitionSpecificationRelationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (definitionSpecificationRelationship == null || definitionSpecificationRelationship.Current.RelationshipType != DatasetRelationshipType.ReleasedData)
            {
                return new StatusCodeResult(404);
            }

            DefinitionSpecificationRelationshipVersion newRelationshipVersion = definitionSpecificationRelationship.Current.DeepCopy(useCamelCase: false);
            newRelationshipVersion.Description = model.Description;
            newRelationshipVersion.LastUpdated = _dateTimeProvider.UtcNow;
            newRelationshipVersion.PublishedSpecificationConfiguration = await CreatePublishedSpecificationConfiguration(specificationId, model.FundingLineIds, model.CalculationIds);
            definitionSpecificationRelationship.Current = newRelationshipVersion = await _relationshipVersionRepository.CreateVersion(newRelationshipVersion, definitionSpecificationRelationship.Current);

            HttpStatusCode statusCode = await _datasetRepositoryPolicy.ExecuteAsync(() => _datasetRepository.SaveDefinitionSpecificationRelationship(definitionSpecificationRelationship));

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save relationship with status code: {statusCode}");
                return new StatusCodeResult((int)statusCode);
            }

            await _relationshipVersionRepository.SaveVersion(newRelationshipVersion);

            Job reMapJob = await _calcsRepository.ReMapSpecificationReference(specificationId, relationshipId);

            if (reMapJob == null)
            {
                string errorMessage = "Failed to queue a specification relationship re-map job";
                _logger.Error(errorMessage);
                return new BadRequestObjectResult(errorMessage);
            }

            return new OkObjectResult(newRelationshipVersion);
        }

        public async Task<IEnumerable<DatasetSpecificationRelationshipViewModel>> GetRelationshipsBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<DefinitionSpecificationRelationship> relationships =
                await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Current.Specification.Id == specificationId);

            if (relationships.IsNullOrEmpty())
            {
                return new DatasetSpecificationRelationshipViewModel[0];
            }

            IList<DatasetSpecificationRelationshipViewModel> relationshipViewModels = new List<DatasetSpecificationRelationshipViewModel>();

            IList<Task<DatasetSpecificationRelationshipViewModel>> tasks = new List<Task<DatasetSpecificationRelationshipViewModel>>();

            IEnumerable<KeyValuePair<string, int>> datasetLatestVersions =
                await _datasetRepository.GetDatasetLatestVersions(relationships.Select(_ => _.Current.DatasetVersion?.Id));

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                KeyValuePair<string, int> datasetLatestVersion = datasetLatestVersions.SingleOrDefault(_ => _.Key == relationship.Current.DatasetVersion?.Id);
                Task<DatasetSpecificationRelationshipViewModel> task = CreateViewModel(relationship, datasetLatestVersion);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            return tasks.Select(m => m.Result);
        }

        public async Task<IActionResult> GetRelationshipsBySpecificationIdResult(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to GetRelationshipsBySpecificationId");

                return new BadRequestObjectResult("No specification id was provided to GetRelationshipsBySpecificationId");
            }

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationshipsBySpecification = await GetRelationshipsBySpecificationId(specificationId);

            return new OkObjectResult(relationshipsBySpecification);
        }

        public async Task<IActionResult> GetDataSourcesByRelationshipId(string relationshipId)
        {
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                _logger.Error("The relationshipId id was not provided to GetDataSourcesByRelationshipId");

                return new BadRequestObjectResult("No relationship id was provided");
            }

            SelectDatasourceModel selectDatasourceModel = new SelectDatasourceModel();

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (relationship == null)
            {
                return new StatusCodeResult(412);
            }

            selectDatasourceModel.RelationshipId = relationship.Id;
            selectDatasourceModel.RelationshipName = relationship.Name;
            selectDatasourceModel.SpecificationId = relationship.Current.Specification.Id;
            selectDatasourceModel.SpecificationName = relationship.Current.Specification.Name;
            selectDatasourceModel.DefinitionId = relationship.Current.DatasetDefinition?.Id;
            selectDatasourceModel.DefinitionName = relationship.Current.DatasetDefinition?.Name;
            selectDatasourceModel.RelationshipType = relationship.Current.RelationshipType;

            IEnumerable<Dataset> datasets;

            if (relationship.Current.RelationshipType == DatasetRelationshipType.Uploaded)
            {
                datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Content.Definition.Id == relationship.Current.DatasetDefinition.Id);
            }
            else
            {
                datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Content.RelationshipId == relationship.Id);

                ApiResponse<SpecificationSummary> sourceSpecification = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(relationship.Current.PublishedSpecificationConfiguration.SpecificationId));
                if (sourceSpecification == null || sourceSpecification.Content == null || sourceSpecification.StatusCode != HttpStatusCode.OK)
                {
                    return new PreconditionFailedResult("Source specification not found");
                }

                selectDatasourceModel.SourceSpecificationId = relationship.Current.PublishedSpecificationConfiguration.SpecificationId;
                selectDatasourceModel.SourceSpecificationName = sourceSpecification.Content.Name;
            }

            if (!datasets.IsNullOrEmpty())
            {
                if (selectDatasourceModel.Datasets == null)
                {
                    EnsureNullDatasetsReturnsEmptyArray(selectDatasourceModel);
                }

                foreach (Dataset dataset in datasets)
                {
                    selectDatasourceModel.Datasets = selectDatasourceModel.Datasets.Concat(new[]
                    {
                        new DatasetVersions
                        {
                            Id = dataset.Id,
                            Name = dataset.Name,
                            Description = dataset.Current?.Description,
                            SelectedVersion = (relationship.Current.DatasetVersion != null && relationship.Current.DatasetVersion.Id == dataset.Id) ? relationship.Current.DatasetVersion.Version : null as int?,
                            Versions = (await _versionDatasetRepository.GetVersions(dataset.Id))?.OrderByDescending(_ => _.Version).Select(m => new DatasetVersionModel
                            {
                                Id = m.Id,
                                DatasetId = m.DatasetId,
                                Version = m.Version,
                                Date = m.Date,
                                Author = m.Author,
                                Comment = m.Comment,
                                Description = m.Description
                            })
                        }
                    });
                }
            }
            else if (selectDatasourceModel.Datasets == null)
            {
                EnsureNullDatasetsReturnsEmptyArray(selectDatasourceModel);
            }

            return new OkObjectResult(selectDatasourceModel);
        }

        private static void EnsureNullDatasetsReturnsEmptyArray(SelectDatasourceModel selectDatasourceModel)
        {
            selectDatasourceModel.Datasets = Enumerable.Empty<DatasetVersions>();
        }

        public async Task<IActionResult> ToggleDatasetRelationship(string relationshipId, bool converterEnabled)
        {
            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            relationship.Current.ConverterEnabled = converterEnabled;

            HttpStatusCode statusCode = await _datasetRepository.UpdateDefinitionSpecificationRelationship(relationship);

            return new StatusCodeResult((int)statusCode);
        }

        public async Task<IActionResult> AssignDatasourceVersionToRelationship(AssignDatasourceModel model,
            Reference user,
            string correlationId)
        {
            if (model == null)
            {
                _logger.Error("Null AssignDatasourceModel was provided to AssignDatasourceVersionToRelationship");
                return new BadRequestObjectResult("Null AssignDatasourceModel was provided");
            }

            Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(model.DatasetId);

            if (dataset == null)
            {
                _logger.Error($"Dataset not found for dataset id: {model.DatasetId}");
                return new StatusCodeResult(412);
            }

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(model.RelationshipId);

            if (relationship == null)
            {
                _logger.Error($"Relationship not found for relationship id: {model.RelationshipId}");
                return new StatusCodeResult(412);
            }

            //  need to save the relationship id here so we can list all dataset versions
            dataset.RelationshipId = model.RelationshipId;

            await _datasetRepository.SaveDataset(dataset);

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(relationship.Current.Specification.Id));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                _logger.Error($"Specification was not found for id {relationship.Current.Specification.Id}");
                return new StatusCodeResult(412);
            }

            SpecModel.SpecificationSummary specification = specificationApiResponse.Content;

            DefinitionSpecificationRelationshipVersion previousRelationshipVersion = relationship.Current.DeepCopy(useCamelCase: false);
            DefinitionSpecificationRelationshipVersion relationshipVersion = relationship.Current;

            relationshipVersion.Author = user;
            relationshipVersion.LastUpdated = _dateTimeProvider.UtcNow;
            relationshipVersion.DatasetVersion = new DatasetRelationshipVersion
            {
                Id = model.DatasetId,
                Version = model.Version
            };

            await UpdateDefinitionSpecificationRelationship(relationship, relationshipVersion, previousRelationshipVersion);

            IEnumerable<CalculationResponseModel> allCalculations = await _calcsRepository.GetCurrentCalculationsBySpecificationId(relationshipVersion.Specification.Id);

            bool generateCalculationAggregations = !allCalculations.IsNullOrEmpty() &&
                                                   SourceCodeHelpers.HasCalculationAggregateFunctionParameters(allCalculations.Select(m => m.SourceCode));

            Trigger trigger = new Trigger
            {
                EntityId = relationship.Id,
                EntityType = nameof(Dataset),
                Message = $"Mapping dataset: '{dataset.Id}' with relationship '{relationship.Id}'"
            };

            JobCreateModel job = new JobCreateModel
            {
                InvokerUserDisplayName = user?.Name,
                InvokerUserId = user?.Id,
                JobDefinitionId = specification.ProviderSource != ProviderSource.FDZ ? JobConstants.DefinitionNames.MapDatasetJob : JobConstants.DefinitionNames.MapFdzDatasetsJob,
                MessageBody = JsonConvert.SerializeObject(dataset),
                Properties = new Dictionary<string, string>
                {
                    {"specification-id", relationshipVersion.Specification.Id},
                    {"relationship-id", relationship.Id},
                    {"user-id", user?.Id},
                    {"user-name", user?.Name},
                },
                SpecificationId = relationshipVersion.Specification.Id,
                Trigger = trigger,
                CorrelationId = correlationId
            };

            Job parentJob = null;

            if (relationshipVersion.IsSetAsProviderData)
            {
                string parentJobDefinition = generateCalculationAggregations ? JobConstants.DefinitionNames.MapScopedDatasetJobWithAggregation : JobConstants.DefinitionNames.MapScopedDatasetJob;

                parentJob = await _jobManagement.QueueJob(new JobCreateModel
                {
                    InvokerUserDisplayName = user?.Name,
                    InvokerUserId = user?.Id,
                    JobDefinitionId = parentJobDefinition,
                    Properties = new Dictionary<string, string>
                    {
                        {"specification-id", relationshipVersion.Specification.Id},
                        {"provider-cache-key", $"{CacheKeys.ScopedProviderSummariesPrefix}{relationshipVersion.Specification.Id}"},
                        {"specification-summary-cache-key", $"{CacheKeys.SpecificationSummaryById}{relationshipVersion.Specification.Id}"}
                    },
                    SpecificationId = relationshipVersion.Specification.Id,
                    Trigger = trigger,
                    CorrelationId = correlationId
                });
            }

            if (parentJob != null)
            {
                job.ParentJobId = parentJob.Id;
                job.Properties.Add("parentJobId", parentJob.Id);
                job.Properties.Add("isScopedJob", bool.TrueString);
            }

            Job childJob = await _jobManagement.QueueJob(job);

            JobCreationResponse jobCreationResponse = new JobCreationResponse()
            {
                JobId = childJob.Id,
            };

            return new OkObjectResult(jobCreationResponse);
        }

        public async Task<IActionResult> GetRelationshipBySpecificationIdAndName(string specificationId,
            string name)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("The specification id was not provided to GetRelationshipsBySpecificationIdAndName");

                return new BadRequestObjectResult("No specification id was provided to GetRelationshipsBySpecificationIdAndName");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.Error("The name was not provided to GetRelationshipsBySpecificationIdAndName");

                return new BadRequestObjectResult("No name was provided to GetRelationshipsBySpecificationIdAndName");
            }

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetRelationshipBySpecificationIdAndName(specificationId, name);

            if (relationship != null)
            {
                return new OkObjectResult(relationship);
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetReferenceRelationshipsBySpecificationId(string specificationId)
        {
            return await GetCurrentRelationshipsByQuery(specificationId, _ => _.Content.Current.RelationshipType == DatasetRelationshipType.ReleasedData && _.Content.Current.PublishedSpecificationConfiguration.SpecificationId == specificationId);
        }

        public async Task<IActionResult> GetCurrentRelationshipsBySpecificationId(string specificationId)
        {
            return await GetCurrentRelationshipsByQuery(specificationId, _ => _.Content.Current.Specification.Id == specificationId);
        }

        private async Task<IActionResult> GetCurrentRelationshipsByQuery(string specificationId, Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>> query)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"No specification id was provided to GetCurrentRelationshipsBySpecificationId");

                return new BadRequestObjectResult($"Null or empty {nameof(specificationId)} provided");
            }

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                _logger.Error($"Failed to find specification for id: {specificationId}");

                return new StatusCodeResult(412);
            }

            IEnumerable<DefinitionSpecificationRelationship> relationships =
                (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(query)).ToList();

            if (relationships.IsNullOrEmpty())
            {
                relationships = new DefinitionSpecificationRelationship[0];
            }

            IList<DatasetSpecificationRelationshipViewModel> relationshipViewModels = new List<DatasetSpecificationRelationshipViewModel>();

            if (relationships.IsNullOrEmpty())
            {
                return new OkObjectResult(relationshipViewModels);
            }

            IEnumerable<KeyValuePair<string, int>> datasetLatestVersions =
                await _datasetRepository.GetDatasetLatestVersions(relationships.Select(_ => _.Current.DatasetVersion?.Id));

            IList<Task<DatasetSpecificationRelationshipViewModel>> tasks = new List<Task<DatasetSpecificationRelationshipViewModel>>();

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                KeyValuePair<string, int> datasetLatestVersion = datasetLatestVersions.SingleOrDefault(_ => _.Key == relationship.Current.DatasetVersion?.Id);
                Task<DatasetSpecificationRelationshipViewModel> task = CreateViewModel(relationship, datasetLatestVersion);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            foreach (Task<DatasetSpecificationRelationshipViewModel> task in tasks)
            {
                relationshipViewModels.Add(task.Result);
            }

            return new OkObjectResult(tasks.Select(m => m.Result));
        }

        public async Task<IActionResult> GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(string specificationId,
            string datasetDefinitionId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(datasetDefinitionId, nameof(datasetDefinitionId));

            IEnumerable<DefinitionSpecificationRelationship> relationships =
                await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Current.Specification.Id == specificationId && m.Content.Current.DatasetDefinition.Id == datasetDefinitionId);

            if (relationships.IsNullOrEmpty())
            {
                relationships = new DefinitionSpecificationRelationship[0];
            }

            IList<DatasetSpecificationRelationshipViewModel> relationshipViewModels = new List<DatasetSpecificationRelationshipViewModel>();

            if (relationships.IsNullOrEmpty())
            {
                return new OkObjectResult(relationshipViewModels);
            }

            IEnumerable<KeyValuePair<string, int>> datasetLatestVersions =
                await _datasetRepository.GetDatasetLatestVersions(relationships.Select(_ => _.Current.DatasetVersion?.Id));

            IList<Task<DatasetSpecificationRelationshipViewModel>> tasks = new List<Task<DatasetSpecificationRelationshipViewModel>>();

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                KeyValuePair<string, int> datasetLatestVersion = datasetLatestVersions.SingleOrDefault(_ => _.Key == relationship.Current.DatasetVersion?.Id);
                Task<DatasetSpecificationRelationshipViewModel> task = CreateViewModel(relationship, datasetLatestVersion);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            foreach (Task<DatasetSpecificationRelationshipViewModel> task in tasks)
            {
                relationshipViewModels.Add(task.Result);
            }

            return new OkObjectResult(tasks.Select(m => m.Result));
        }

        public async Task<IActionResult> GetCurrentDatasetRelationshipFieldsBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<DefinitionSpecificationRelationship> relationships =
                (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Current.Specification.Id == specificationId)).ToList();

            if (relationships.IsNullOrEmpty())
            {
                relationships = Array.Empty<DefinitionSpecificationRelationship>();
            }

            IEnumerable<string> definitionIds = relationships.Select(m => m.Current.DatasetDefinition.Id);

            IEnumerable<DatasetDefinition> definitions = await _datasetRepository.GetDatasetDefinitionsByQuery(d => definitionIds.Contains(d.Id));

            IList<DatasetSchemaRelationshipModel> schemaRelationshipModels = new List<DatasetSchemaRelationshipModel>();

            foreach (DefinitionSpecificationRelationship definitionSpecificationRelationship in relationships)
            {
                string relationshipName = definitionSpecificationRelationship.Name;
                string datasetName = _typeIdentifierGenerator.GenerateIdentifier(relationshipName);
                DatasetDefinition datasetDefinition = definitions.FirstOrDefault(m => m.Id == definitionSpecificationRelationship.Current.DatasetDefinition.Id);

                schemaRelationshipModels.Add(new DatasetSchemaRelationshipModel
                {
                    DefinitionId = datasetDefinition.Id,
                    RelationshipId = definitionSpecificationRelationship.Id,
                    RelationshipName = relationshipName,
                    Fields = datasetDefinition.TableDefinitions.SelectMany(m => m.FieldDefinitions.Select(f => new DatasetSchemaRelationshipField
                    {
                        Name = f.Name,
                        SourceName = _typeIdentifierGenerator.GenerateIdentifier(f.Name),
                        SourceRelationshipName = datasetName,
                        IsAggregable = f.IsAggregable
                    }))
                });
            }

            return new OkObjectResult(schemaRelationshipModels);
        }

        public async Task<IActionResult> GetSpecificationIdsForRelationshipDefinitionId(string datasetDefinitionId)
        {
            Guard.IsNullOrWhiteSpace(datasetDefinitionId, nameof(datasetDefinitionId));

            IEnumerable<string> specificationIds = await _datasetRepository.GetDistinctRelationshipSpecificationIdsForDatasetDefinitionId(datasetDefinitionId);

            return new OkObjectResult(specificationIds);
        }

        public async Task UpdateRelationshipDatasetDefinitionName(Reference datasetDefinitionReference)
        {
            if (datasetDefinitionReference == null)
            {
                _logger.Error("Null dataset definition reference supplied");
                throw new NonRetriableException("A null dataset definition reference was supplied");
            }

            IEnumerable<DefinitionSpecificationRelationship> relationships =
                (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Current.DatasetDefinition.Id == datasetDefinitionReference.Id)).ToList();

            if (!relationships.IsNullOrEmpty())
            {
                int relationshipCount = relationships.Count();

                _logger.Information($"Updating {relationshipCount} relationships with new definition name: {datasetDefinitionReference.Name}");

                try
                {
                    foreach (DefinitionSpecificationRelationship definitionSpecificationRelationship in relationships)
                    {
                        DefinitionSpecificationRelationshipVersion previousRelationshipVersion = definitionSpecificationRelationship.Current;
                        DefinitionSpecificationRelationshipVersion relationshipVersion = previousRelationshipVersion.DeepCopy(useCamelCase: false);

                        relationshipVersion.DatasetDefinition.Name = datasetDefinitionReference.Name;

                        await UpdateDefinitionSpecificationRelationship(definitionSpecificationRelationship, relationshipVersion, previousRelationshipVersion);
                    }

                    _logger.Information($"Updated {relationshipCount} relationships with new definition name: {datasetDefinitionReference.Name}");

                }
                catch (Exception ex)
                {
                    _logger.Error(ex, ex.Message);

                    throw new RetriableException($"Failed to update relationships with new definition name: {datasetDefinitionReference.Name}", ex);
                }
            }
            else
            {
                _logger.Information($"No relationships found to update");
            }
        }

        public async Task<IActionResult> ValidateRelationship(ValidateDefinitionSpecificationRelationshipModel model)
        {
            if (model == null)
            {
                _logger.Error("Null ValidateDefinitionSpecificationRelationshipModel was provided to CreateRelationship");
                return new BadRequestObjectResult("Null ValidateDefinitionSpecificationRelationshipModel was provided");
            }

            BadRequestObjectResult validationResult = (await _validateRelationshipModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            return new OkResult();
        }

        public async Task<IActionResult> Migrate()
        {
            IList<OldDefinitionSpecificationRelationship> relationshipsToMigrate = (await _datasetRepository.GetDefinitionSpecificationRelationshipsToMigrate()).ToList();

            _logger.Information($"Number of relationships to migrate - {relationshipsToMigrate.Count}");

            foreach (OldDefinitionSpecificationRelationship oldRelationship in relationshipsToMigrate)
            {
                try
                {
                    DefinitionSpecificationRelationshipVersion relationshipVersion = new DefinitionSpecificationRelationshipVersion()
                    {
                        RelationshipId = oldRelationship.Id,
                        Name = oldRelationship.Content.Name,
                        ConverterEnabled = oldRelationship.Content.ConverterEnabled,
                        DatasetDefinition = oldRelationship.Content.DatasetDefinition,
                        DatasetVersion = oldRelationship.Content.DatasetVersion,
                        Description = oldRelationship.Content.Description,
                        IsSetAsProviderData = oldRelationship.Content.IsSetAsProviderData,
                        LastUpdated = oldRelationship.Content.LastUpdated,
                        Specification = oldRelationship.Content.Specification,
                        UsedInDataAggregations = oldRelationship.Content.UsedInDataAggregations,
                        Author = oldRelationship.Content.Author,
                        RelationshipType = DatasetRelationshipType.Uploaded,
                        PublishedSpecificationConfiguration = oldRelationship.Content.PublishedSpecificationConfiguration,
                    };

                    DefinitionSpecificationRelationship relationship = new DefinitionSpecificationRelationship()
                    {
                        Id = oldRelationship.Id,
                        Name = oldRelationship.Content.Name
                    };

                    await UpdateDefinitionSpecificationRelationship(relationship, relationshipVersion, null);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error occurred while migrating relationship - {oldRelationship.Name}");
                    throw;
                }
            }

            _logger.Information($"Number of relationships migrated - {relationshipsToMigrate.Count}");
            return new NoContentResult();
        }

        public async Task<IActionResult> GetFundingLineCalculations(string relationshipId)
        {
            Guard.ArgumentNotNull(relationshipId, nameof(relationshipId));

            DefinitionSpecificationRelationship definitionSpecificationRelationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (definitionSpecificationRelationship == null)
            {
                return new StatusCodeResult(404);
            }
            else if (definitionSpecificationRelationship.Current.RelationshipType != DatasetRelationshipType.ReleasedData)
            {
                return new StatusCodeResult(412);
            }

            string specificationId = definitionSpecificationRelationship.Current.Specification.Id;
            PublishedSpecificationConfiguration publishedSpecificationConfiguration = await CreatePublishedSpecificationConfiguration(
                specificationId,
                definitionSpecificationRelationship.Current.PublishedSpecificationConfiguration);

            return new OkObjectResult(publishedSpecificationConfiguration);
        }

        private async Task<DatasetSpecificationRelationshipViewModel> CreateViewModel(
            DefinitionSpecificationRelationship relationship,
            KeyValuePair<string, int> datasetLatestVersion)
        {
            DefinitionSpecificationRelationshipVersion relationshipVersion = relationship.Current;

            DatasetSpecificationRelationshipViewModel relationshipViewModel = new DatasetSpecificationRelationshipViewModel
            {
                Id = relationship.Id,
                Name = relationship.Name,
                RelationshipDescription = relationshipVersion.Description,
                IsProviderData = relationshipVersion.IsSetAsProviderData,
                LastUpdatedAuthor = relationshipVersion.Author,
                LastUpdatedDate = relationshipVersion.LastUpdated,
                ConverterEnabled = relationshipVersion.ConverterEnabled,
                RelationshipType = relationshipVersion.RelationshipType,
                PublishedSpecificationConfiguration = relationshipVersion.PublishedSpecificationConfiguration
            };

            if (relationshipVersion.DatasetVersion != null)
            {
                Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(relationshipVersion.DatasetVersion.Id);

                if (dataset != null)
                {
                    relationshipViewModel.DatasetId = relationshipVersion.DatasetVersion.Id;
                    relationshipViewModel.DatasetName = dataset.Name;
                    relationshipViewModel.Version = relationshipVersion.DatasetVersion.Version;
                    relationshipViewModel.IsLatestVersion = relationshipVersion.DatasetVersion.Version == datasetLatestVersion.Value;
                }
                else
                {
                    _logger.Warning($"Dataset could not be found for Id {relationshipVersion.DatasetVersion.Id}");
                }
            }

            if (relationshipVersion.DatasetDefinition != null)
            {
                string definitionId = relationshipVersion.DatasetDefinition.Id;

                DatasetDefinition definition = await _datasetRepository.GetDatasetDefinition(definitionId);

                if (definition != null)
                {
                    relationshipViewModel.Definition = new DatasetDefinitionViewModel
                    {
                        Id = definition.Id,
                        Name = definition.Name,
                        Description = definition.Description
                    };

                    relationshipViewModel.ConverterEligible = definition.ConverterEligible;
                }
            }

            return relationshipViewModel;
        }

        private async Task UpdateDefinitionSpecificationRelationship(
            DefinitionSpecificationRelationship definitionSpecificationRelationship,
            DefinitionSpecificationRelationshipVersion relationshipVersion,
            DefinitionSpecificationRelationshipVersion previousRelationshipVersion)
        {
            relationshipVersion = await _relationshipRepositoryPolicy.ExecuteAsync(() => _relationshipVersionRepository.CreateVersion(relationshipVersion, previousRelationshipVersion));
            definitionSpecificationRelationship.Current = relationshipVersion;

            HttpStatusCode statusCode = await _datasetRepositoryPolicy.ExecuteAsync(() => _datasetRepository.UpdateDefinitionSpecificationRelationship(definitionSpecificationRelationship));

            if (!statusCode.IsSuccess())
            {
                string message = $"Failed to save relationship - {definitionSpecificationRelationship.Name} with status code: {statusCode.ToString()}";
                _logger.Error(message);
                throw new RetriableException(message);
            }

            await _relationshipRepositoryPolicy.ExecuteAsync(() => _relationshipVersionRepository.SaveVersion(relationshipVersion));
        }

        private async Task<PublishedSpecificationConfiguration> CreatePublishedSpecificationConfiguration(string targetSpecificationId,
            PublishedSpecificationConfiguration originalPublishedSpecificationConfiguration)
        {
            List<PublishedSpecificationItem> GetFundingLines(TemplateMetadataDistinctContents metadata, string templateId)
            {
                List<PublishedSpecificationItem> fundingLines = new List<PublishedSpecificationItem>();
                IEnumerable<PublishedSpecificationItem> currentFundingLines = originalPublishedSpecificationConfiguration.FundingLines;

                foreach (TemplateMetadataFundingLine fundingLine in metadata.FundingLines)
                {
                    PublishedSpecificationItem currentFundingLine = currentFundingLines.FirstOrDefault(x => x.TemplateId == fundingLine.TemplateLineId);
                    if (currentFundingLine != null)
                    {
                        fundingLines.Add(new PublishedSpecificationItem()
                        {
                            TemplateId = currentFundingLine.TemplateId,
                            Name = currentFundingLine.Name,
                            SourceCodeName = currentFundingLine.SourceCodeName,
                            IsSelected = true
                        });
                    }
                    else
                    {
                        fundingLines.Add(new PublishedSpecificationItem()
                        {
                            TemplateId = fundingLine.TemplateLineId,
                            Name = fundingLine.Name,
                            SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLine.Name)
                        });
                    }
                }

                IEnumerable<PublishedSpecificationItem> obsoleteFundingLines = currentFundingLines.Where(fl => !metadata.FundingLines.Any(m => m.TemplateLineId == fl.TemplateId));
                foreach (PublishedSpecificationItem obsoleteFundingLine in obsoleteFundingLines)
                {
                    fundingLines.Add(new PublishedSpecificationItem()
                    {
                        TemplateId = obsoleteFundingLine.TemplateId,
                        Name = obsoleteFundingLine.Name,
                        SourceCodeName = obsoleteFundingLine.SourceCodeName,
                        IsSelected = true,
                        IsObsolete = true
                    });
                }

                return fundingLines;
            }

            List<PublishedSpecificationItem> GetCalculations(TemplateMetadataDistinctContents metadata, string templateId)
            {
                List<PublishedSpecificationItem> calculations = new List<PublishedSpecificationItem>();
                IEnumerable<PublishedSpecificationItem> currentCalculations = originalPublishedSpecificationConfiguration.Calculations;

                foreach (TemplateMetadataCalculation calculation in metadata.Calculations)
                {
                    PublishedSpecificationItem currentCalculation = currentCalculations.FirstOrDefault(x => x.TemplateId == calculation.TemplateCalculationId);
                    if (currentCalculation != null)
                    {
                        calculations.Add(new PublishedSpecificationItem()
                        {
                            TemplateId = currentCalculation.TemplateId,
                            Name = currentCalculation.Name,
                            SourceCodeName = currentCalculation.SourceCodeName,
                            IsSelected = true
                        });
                    }
                    else
                    {
                        calculations.Add(new PublishedSpecificationItem()
                        {
                            TemplateId = calculation.TemplateCalculationId,
                            Name = calculation.Name,
                            SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculation.Name)
                        });
                    }
                }

                IEnumerable<PublishedSpecificationItem> obsoleteCalculations = currentCalculations.Where(fl => !metadata.Calculations.Any(m => m.TemplateCalculationId == fl.TemplateId));
                foreach (PublishedSpecificationItem obsoleteCalculation in obsoleteCalculations)
                {
                    calculations.Add(new PublishedSpecificationItem()
                    {
                        TemplateId = obsoleteCalculation.TemplateId,
                        Name = obsoleteCalculation.Name,
                        SourceCodeName = obsoleteCalculation.SourceCodeName,
                        IsSelected = true,
                        IsObsolete = true
                    });
                }

                return calculations;
            }

            return await BuildPublishedSpecificationConfiguration(targetSpecificationId, GetFundingLines, GetCalculations);
        }

        private async Task<PublishedSpecificationConfiguration> CreatePublishedSpecificationConfiguration(string targetSpecificationId,
            IEnumerable<uint> fundingLineIds, IEnumerable<uint> calculationIds)
        {
            List<PublishedSpecificationItem> GetFundingLines(TemplateMetadataDistinctContents metadata, string templateId)
            {
                List<PublishedSpecificationItem> fundingLines = new List<PublishedSpecificationItem>();

                if (!fundingLineIds.IsNullOrEmpty())
                {
                    foreach (uint fundingLineId in fundingLineIds.Distinct())
                    {
                        TemplateMetadataFundingLine fundingLineMetadata = metadata.FundingLines?.FirstOrDefault(x => x.TemplateLineId == fundingLineId);

                        if (fundingLineMetadata != null)
                        {
                            fundingLines.Add(new PublishedSpecificationItem()
                            {
                                TemplateId = fundingLineId,
                                Name = fundingLineMetadata.Name,
                                SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineMetadata.Name)
                            });
                        }
                        else
                        {
                            string errorMessage = $"No fundingline id '{fundingLineId}' in the metadata for FundingStreamId={metadata.FundingStreamId}, FundingPeriodId={metadata.FundingPeriodId} and TemplateId={templateId}.";
                            _logger.Error(errorMessage);
                            throw new NonRetriableException(errorMessage);
                        }
                    }
                }

                return fundingLines;
            }

            List<PublishedSpecificationItem> GetCalculations(TemplateMetadataDistinctContents metadata, string templateId)
            {
                List<PublishedSpecificationItem> calculations = new List<PublishedSpecificationItem>();

                if (!calculationIds.IsNullOrEmpty())
                {
                    foreach (uint calculationId in calculationIds.Distinct())
                    {
                        TemplateMetadataCalculation calculationMetadata = metadata.Calculations?.FirstOrDefault(x => x.TemplateCalculationId == calculationId);

                        if (calculationMetadata != null)
                        {
                            calculations.Add(new PublishedSpecificationItem()
                            {
                                TemplateId = calculationId,
                                Name = calculationMetadata.Name,
                                SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculationMetadata.Name),
                                FieldType = GetFieldType(calculationMetadata.Type)
                            });
                        }
                        else
                        {
                            string errorMessage = $"No calculation id '{calculationId}' in the metadata for FundingStreamId={metadata.FundingStreamId}, FundingPeriodId={metadata.FundingPeriodId} and TemplateId={templateId}.";
                            _logger.Error(errorMessage);
                            throw new NonRetriableException(errorMessage);
                        }
                    }
                }

                return calculations;
            }

            return await BuildPublishedSpecificationConfiguration(targetSpecificationId, GetFundingLines, GetCalculations);
        }

        private async Task<PublishedSpecificationConfiguration> BuildPublishedSpecificationConfiguration(string specificationId,
            Func<TemplateMetadataDistinctContents, string, List<PublishedSpecificationItem>> GetFundingLines,
            Func<TemplateMetadataDistinctContents, string, List<PublishedSpecificationItem>> GetCalculations)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                return null;
            }

            ApiResponse<SpecificationSummary> specificationSummaryApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (!specificationSummaryApiResponse.StatusCode.IsSuccess() && specificationSummaryApiResponse.StatusCode != HttpStatusCode.NotFound)
            {
                string errorMessage = $"Failed to fetch specification summary for specification ID: {specificationId} with StatusCode={specificationSummaryApiResponse.StatusCode}";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            SpecificationSummary specificationSummary = specificationSummaryApiResponse.Content;
            string fundingStreamId = specificationSummary.FundingStreams.First().Id;
            string fundingPeriodId = specificationSummary.FundingPeriod.Id;
            string templateId = specificationSummary.TemplateIds.First(x => x.Key == fundingStreamId).Value;

            ApiResponse<TemplateMetadataDistinctContents> metadataResponse =
                    await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateId));

            if (!metadataResponse.StatusCode.IsSuccess() && metadataResponse.StatusCode != HttpStatusCode.NotFound)
            {
                string errorMessage = $"Failed to fetch template metadata for FundingStreamId={fundingStreamId}, FundingPeriodId={fundingPeriodId} and TemplateId={templateId} with StatusCode={metadataResponse.StatusCode}";
                _logger.Error(errorMessage);
                throw new RetriableException(errorMessage);
            }

            TemplateMetadataDistinctContents metadata = metadataResponse.Content;

            return new PublishedSpecificationConfiguration()
            {
                SpecificationId = specificationId,
                FundingStreamId = fundingStreamId,
                FundingPeriodId = fundingPeriodId,
                FundingLines = GetFundingLines(metadata, templateId),
                Calculations = GetCalculations(metadata, templateId)
            };
        }

        private static FieldType GetFieldType(TemplateMetadataCalculationType calculationType)
        {
            switch (calculationType)
            {
                case TemplateMetadataCalculationType.Adjustment:
                case TemplateMetadataCalculationType.Cash:
                case TemplateMetadataCalculationType.Rate:
                case TemplateMetadataCalculationType.PupilNumber:
                case TemplateMetadataCalculationType.Weighting:
                case TemplateMetadataCalculationType.PerPupilFunding:
                case TemplateMetadataCalculationType.LumpSum:
                case TemplateMetadataCalculationType.Number:
                    return FieldType.NullableOfDecimal;
                case TemplateMetadataCalculationType.Boolean:
                    return FieldType.NullableOfBoolean;
                case TemplateMetadataCalculationType.Enum:
                    return FieldType.String;
                default:
                    throw new NotSupportedException($"Unknown calculation type - {calculationType.ToString()} to get the field type");
            }
        }
    }
}