using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
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
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionSpecificationRelationshipService : 
        IDefinitionSpecificationRelationshipService, IHealthChecker
    {
        private readonly IDatasetRepository _datasetRepository;
        private readonly ILogger _logger;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IValidator<CreateDefinitionSpecificationRelationshipModel> _relationshipModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly IDatasetService _datasetService;
        private readonly ICalcsRepository _calcsRepository;
        private readonly IDefinitionsService _definitionService;
        private readonly ICacheProvider _cacheProvider;
        private readonly IJobManagement _jobManagement;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;

        public DefinitionSpecificationRelationshipService(IDatasetRepository datasetRepository,
            ILogger logger,
            ISpecificationsApiClient specificationsApiClient,
            IValidator<CreateDefinitionSpecificationRelationshipModel> relationshipModelValidator,
            IMessengerService messengerService,
            IDatasetService datasetService,
            ICalcsRepository calcsRepository,
            IDefinitionsService definitionService,
            ICacheProvider cacheProvider,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(relationshipModelValidator, nameof(relationshipModelValidator));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(definitionService, nameof(definitionService));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.SpecificationsApiClient, nameof(datasetsResiliencePolicies.SpecificationsApiClient));

            _datasetRepository = datasetRepository;
            _logger = logger;
            _specificationsApiClient = specificationsApiClient;
            _relationshipModelValidator = relationshipModelValidator;
            _messengerService = messengerService;
            _datasetService = datasetService;
            _calcsRepository = calcsRepository;
            _definitionService = definitionService;
            _cacheProvider = cacheProvider;
            _jobManagement = jobManagement;
            _specificationsApiClientPolicy = datasetsResiliencePolicies.SpecificationsApiClient;
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
            health.Dependencies.Add(new DependencyHealth { HealthOk = messengerServiceHealth.Ok, DependencyName = $"{_messengerService.GetType().GetFriendlyName()} for queue: {queueName}", Message = messengerServiceHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }

        public async Task<IActionResult> CreateRelationship(CreateDefinitionSpecificationRelationshipModel model, Reference author, string correlationId)
        {
            if (model == null)
            {
                _logger.Error("Null CreateDefinitionSpecificationRelationshipModel was provided to CreateRelationship");
                return new BadRequestObjectResult("Null CreateDefinitionSpecificationRelationshipModel was provided");
            }

            BadRequestObjectResult validationResult = (await _relationshipModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            DatasetDefinition definition = await _datasetRepository.GetDatasetDefinition(model.DatasetDefinitionId);

            if (definition == null)
            {
                _logger.Error($"Datset definition was not found for id {model.DatasetDefinitionId}");
                return new StatusCodeResult(412);
            }

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(model.SpecificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                _logger.Error($"Specification was not found for id {model.SpecificationId}");
                return new StatusCodeResult(412);
            }

            SpecModel.SpecificationSummary specification = specificationApiResponse.Content;

            string relationshipId = Guid.NewGuid().ToString();

            DefinitionSpecificationRelationship relationship = new DefinitionSpecificationRelationship
            {
                Name = model.Name,
                DatasetDefinition = new Reference(definition.Id, definition.Name),
                Specification = new Reference(specification.Id, specification.Name),
                Description = model.Description,
                Id = relationshipId,
                IsSetAsProviderData = model.IsSetAsProviderData,
                UsedInDataAggregations = model.UsedInDataAggregations
            };

            HttpStatusCode statusCode = await _datasetRepository.SaveDefinitionSpecificationRelationship(relationship);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save relationship with status code: {statusCode.ToString()}");
                return new StatusCodeResult((int)statusCode);
            }

            IDictionary<string, string> properties = MessageExtensions.BuildMessageProperties(correlationId, author);

            await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification,
                new AssignDefinitionRelationshipMessage
                {
                    SpecificationId = specification.Id,
                    RelationshipId = relationshipId
                },
                properties);


            DatasetRelationshipSummary relationshipSummary = new DatasetRelationshipSummary
            {
                Name = relationship.Name,
                Id = Guid.NewGuid().ToString(),
                Relationship = new Reference(relationship.Id, relationship.Name),
                DatasetDefinition = definition,
                DatasetDefinitionId = definition.Id,
                DataGranularity = relationship.UsedInDataAggregations ? DataGranularity.MultipleRowsPerProvider : DataGranularity.SingleRowPerProvider,
                DefinesScope = relationship.IsSetAsProviderData
            };

            await _calcsRepository.UpdateBuildProjectRelationships(specification.Id, relationshipSummary);

            await _cacheProvider.RemoveAsync<IEnumerable<DatasetSchemaRelationshipModel>>($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{specification.Id}");

            return new OkObjectResult(relationship);
        }

        public async Task<IActionResult> GetRelationshipsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to GetRelationshipsBySpecificationId");

                return new BadRequestObjectResult("No specification id was provided to GetRelationshipsBySpecificationId");
            }

            IEnumerable<DefinitionSpecificationRelationship> relationships =
                await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Specification.Id == specificationId);

            if (relationships.IsNullOrEmpty())
            {
                relationships = new DefinitionSpecificationRelationship[0];
            }

            IList<DatasetSpecificationRelationshipViewModel> relationshipViewModels = new List<DatasetSpecificationRelationshipViewModel>();

            if (relationships.IsNullOrEmpty())
            {
                return new OkObjectResult(relationshipViewModels);
            }

            IList<Task<DatasetSpecificationRelationshipViewModel>> tasks = new List<Task<DatasetSpecificationRelationshipViewModel>>();

            IEnumerable<KeyValuePair<string, int>> datasetLatestVersions =
                await _datasetRepository.GetDatasetLatestVersions(relationships.Select(_ => _.DatasetVersion?.Id));

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                KeyValuePair<string, int> datasetLatestVersion = datasetLatestVersions.SingleOrDefault(_ => _.Key == relationship.DatasetVersion?.Id);
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
            selectDatasourceModel.SpecificationId = relationship.Specification.Id;
            selectDatasourceModel.SpecificationName = relationship.Specification.Name;
            selectDatasourceModel.DefinitionId = relationship.DatasetDefinition.Id;
            selectDatasourceModel.DefinitionName = relationship.DatasetDefinition.Name;

            IEnumerable<Dataset> datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Content.Definition.Id == relationship.DatasetDefinition.Id);

            if (!datasets.IsNullOrEmpty())
            {
                if (selectDatasourceModel.Datasets == null)
                {
                    selectDatasourceModel.Datasets = Enumerable.Empty<DatasetVersions>();
                }

                foreach (Dataset dataset in datasets)
                {
                    selectDatasourceModel.Datasets = selectDatasourceModel.Datasets.Concat(new[]{
                        new DatasetVersions
                        {
                            Id = dataset.Id,
                            Name = dataset.Name,
                            SelectedVersion = (relationship.DatasetVersion != null && relationship.DatasetVersion.Id == dataset.Id) ? relationship.DatasetVersion.Version : null as int?,
                            Versions = dataset.History.Select(m => new DatasetVersionModel
                            { 
                                Id = m.Id,
                                Version = m.Version,
                                Date = m.Date,
                                Author = m.Author,
                                Comment = m.Comment
                            } )
                        }
                });
                }
            }
            return new OkObjectResult(selectDatasourceModel);
        }

        public async Task<IActionResult> AssignDatasourceVersionToRelationship(AssignDatasourceModel model, Reference user, string correlationId)
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

            relationship.DatasetVersion = new DatasetRelationshipVersion
            {
                Id = model.DatasetId,
                Version = model.Version
            };

            HttpStatusCode statusCode = await _datasetRepository.UpdateDefinitionSpecificationRelationship(relationship);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to assign data source to relationship : {model.RelationshipId} with status code {statusCode.ToString()}");

                return new StatusCodeResult((int)statusCode);
            }

            string jobToQueue = relationship.IsSetAsProviderData ? JobConstants.DefinitionNames.MapScopedDatasetJob : JobConstants.DefinitionNames.MapDatasetJob;
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
                JobDefinitionId = jobToQueue,
                MessageBody = JsonConvert.SerializeObject(dataset),
                Properties = new Dictionary<string, string>
                {
                    { "specification-id", relationship.Specification.Id },
                    { "relationship-id", relationship.Id },
                    { "user-id", user?.Id},
                    { "user-name", user?.Name},
                },
                SpecificationId = relationship.Specification.Id,
                Trigger = trigger,
                CorrelationId = correlationId
            };

            await _jobManagement.QueueJob(job);
            
            return new NoContentResult();
        }

        public async Task<IActionResult> GetRelationshipBySpecificationIdAndName(string specificationId, string name)
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

        public async Task<IActionResult> GetCurrentRelationshipsBySpecificationId(string specificationId)
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
               (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Specification.Id == specificationId)).ToList();

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
                await _datasetRepository.GetDatasetLatestVersions(relationships.Select(_ => _.DatasetVersion?.Id));

            IList<Task<DatasetSpecificationRelationshipViewModel>> tasks = new List<Task<DatasetSpecificationRelationshipViewModel>>();

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                KeyValuePair<string, int> datasetLatestVersion = datasetLatestVersions.SingleOrDefault(_ => _.Key == relationship.DatasetVersion?.Id);
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

        public async Task<IActionResult> GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(string specificationId, string datasetDefinitionId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(datasetDefinitionId, nameof(datasetDefinitionId));

            IEnumerable<DefinitionSpecificationRelationship> relationships =
               await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Specification.Id == specificationId && m.Content.DatasetDefinition.Id == datasetDefinitionId);

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
                await _datasetRepository.GetDatasetLatestVersions(relationships.Select(_ => _.DatasetVersion?.Id));

            IList<Task<DatasetSpecificationRelationshipViewModel>> tasks = new List<Task<DatasetSpecificationRelationshipViewModel>>();

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                KeyValuePair<string, int> datasetLatestVersion = datasetLatestVersions.SingleOrDefault(_ => _.Key == relationship.DatasetVersion?.Id);
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
               (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Specification.Id == specificationId)).ToList();

            if (relationships.IsNullOrEmpty())
            {
                relationships = Array.Empty<DefinitionSpecificationRelationship>();
            }

            IEnumerable<string> definitionIds = relationships.Select(m => m.DatasetDefinition.Id);

            IEnumerable<DatasetDefinition> definitions = await _datasetRepository.GetDatasetDefinitionsByQuery(d => definitionIds.Contains(d.Id));

            IList<DatasetSchemaRelationshipModel> schemaRelationshipModels = new List<DatasetSchemaRelationshipModel>();

            foreach (DefinitionSpecificationRelationship definitionSpecificationRelationship in relationships)
            {
                string relationshipName = definitionSpecificationRelationship.Name;
                string datasetName = VisualBasicTypeGenerator.GenerateIdentifier(relationshipName);
                DatasetDefinition datasetDefinition = definitions.FirstOrDefault(m => m.Id == definitionSpecificationRelationship.DatasetDefinition.Id);

                schemaRelationshipModels.Add(new DatasetSchemaRelationshipModel
                {
                    DefinitionId = datasetDefinition.Id,
                    RelationshipId = definitionSpecificationRelationship.Id,
                    RelationshipName = relationshipName,
                    Fields = datasetDefinition.TableDefinitions.SelectMany(m => m.FieldDefinitions.Select(f => new DatasetSchemaRelationshipField
                    {
                        Name = f.Name,
                        SourceName = VisualBasicTypeGenerator.GenerateIdentifier(f.Name),
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

        public async Task UpdateRelationshipDatasetDefinitionName(Reference datsetDefinitionReference)
        {
            if (datsetDefinitionReference == null)
            {
                _logger.Error("Null dataset definition reference supplied");
                throw new NonRetriableException("A null dataset definition reference was supplied");
            }

            IEnumerable<DefinitionSpecificationRelationship> relationships =
              (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.DatasetDefinition.Id == datsetDefinitionReference.Id)).ToList();

            if (!relationships.IsNullOrEmpty())
            {
                int relationshipCount = relationships.Count();

                _logger.Information($"Updating {relationshipCount} relationships with new definition name: {datsetDefinitionReference.Name}");

                try
                {

                    foreach (DefinitionSpecificationRelationship definitionSpecificationRelationship in relationships)
                    {
                        definitionSpecificationRelationship.DatasetDefinition.Name = datsetDefinitionReference.Name;
                    }

                    await _datasetRepository.UpdateDefinitionSpecificationRelationships(relationships);

                    _logger.Information($"Updated {relationshipCount} relationships with new definition name: {datsetDefinitionReference.Name}");

                }
                catch (Exception ex)
                {
                    _logger.Error(ex, ex.Message);

                    throw new RetriableException($"Failed to update relationships with new definition name: {datsetDefinitionReference.Name}", ex);
                }
            }
            else
            {
                _logger.Information($"No relationships found to update");
            }
        }

        private async Task<DatasetSpecificationRelationshipViewModel> CreateViewModel(
            DefinitionSpecificationRelationship relationship,
            KeyValuePair<string, int> datasetLatestVersion)
        {
            DatasetSpecificationRelationshipViewModel relationshipViewModel = new DatasetSpecificationRelationshipViewModel
            {
                Id = relationship.Id,
                Name = relationship.Name,
                RelationshipDescription = relationship.Description,
                IsProviderData = relationship.IsSetAsProviderData
            };

            if (relationship.DatasetVersion != null)
            {
                Dataset dataset = await _datasetRepository.GetDatasetByDatasetId(relationship.DatasetVersion.Id);

                if (dataset != null)
                {
                    relationshipViewModel.DatasetId = relationship.DatasetVersion.Id;
                    relationshipViewModel.DatasetName = dataset.Name;
                    relationshipViewModel.Version = relationship.DatasetVersion.Version;
                    relationshipViewModel.IsLatestVersion = relationship.DatasetVersion.Version == datasetLatestVersion.Value;
                }
                else
                {
                    _logger.Warning($"Dataset could not be found for Id {relationship.DatasetVersion.Id}");
                }
            }

            if (relationship.DatasetDefinition != null)
            {
                string definitionId = relationship.DatasetDefinition.Id;

                Models.Datasets.Schema.DatasetDefinition definition = await _datasetRepository.GetDatasetDefinition(definitionId);

                if (definition != null)
                {
                    relationshipViewModel.Definition = new DatasetDefinitionViewModel
                    {
                        Id = definition.Id,
                        Name = definition.Name,
                        Description = definition.Description
                    };
                }
            }

            return relationshipViewModel;
        }
    }
}
