using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionSpecificationRelationshipService : IDefinitionSpecificationRelationshipService, IHealthChecker
    {
        private readonly IDatasetRepository _datasetRepository;
        private readonly ILogger _logger;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IValidator<CreateDefinitionSpecificationRelationshipModel> _relationshipModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly IDatasetService _datasetService;
        private readonly ICalcsRepository _calcsRepository;
        private readonly IDefinitionsService _definitionService;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.Policy _jobsApiClientPolicy;
        private readonly IJobsApiClient _jobsApiClient;

        public DefinitionSpecificationRelationshipService(IDatasetRepository datasetRepository,
            ILogger logger,
            ISpecificationsRepository specificationsRepository,
            IValidator<CreateDefinitionSpecificationRelationshipModel> relationshipModelValidator,
            IMessengerService messengerService,
            IDatasetService datasetService,
            ICalcsRepository calcsRepository,
            IDefinitionsService definitionService,
            ICacheProvider cacheProvider,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IJobsApiClient jobsApiClient)
        {
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(relationshipModelValidator, nameof(relationshipModelValidator));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(definitionService, nameof(definitionService));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(datasetsResiliencePolicies, nameof(datasetsResiliencePolicies));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));

            _datasetRepository = datasetRepository;
            _logger = logger;
            _specificationsRepository = specificationsRepository;
            _relationshipModelValidator = relationshipModelValidator;
            _messengerService = messengerService;
            _datasetService = datasetService;
            _calcsRepository = calcsRepository;
            _definitionService = definitionService;
            _cacheProvider = cacheProvider;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = datasetsResiliencePolicies.JobsApiClient;
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

        public async Task<IActionResult> CreateRelationship(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CreateDefinitionSpecificationRelationshipModel model = JsonConvert.DeserializeObject<CreateDefinitionSpecificationRelationshipModel>(json);

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

            SpecificationSummary specification = await _specificationsRepository.GetSpecificationSummaryById(model.SpecificationId);

            if (specification == null)
            {
                _logger.Error($"Specification was not found for id {model.SpecificationId}");
                return new StatusCodeResult(412);
            }

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

            IDictionary<string, string> properties = request.BuildMessageProperties();

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
                DataGranularity = relationship.UsedInDataAggregations ? DataGranularity.MultipleRowsPerProvider : DataGranularity.SingleRowPerProvider,
                DefinesScope = relationship.IsSetAsProviderData
            };

            BuildProject buildProject = await _calcsRepository.UpdateBuildProjectRelationships(specification.Id, relationshipSummary);

            await _cacheProvider.RemoveAsync<IEnumerable<DatasetSchemaRelationshipModel>>($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{specification.Id}");

            return new OkObjectResult(relationship);
        }

        public async Task<IActionResult> GetRelationshipsBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out Microsoft.Extensions.Primitives.StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to GetRelationshipsBySpecificationId");

                return new BadRequestObjectResult("No specification id was provided to GetRelationshipsBySpecificationId");
            }

            IEnumerable<DefinitionSpecificationRelationship> relationships =
                await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Specification.Id == specificationId);

            if (relationships.IsNullOrEmpty())
            {
                relationships = new DefinitionSpecificationRelationship[0];
            }

            return new OkObjectResult(relationships);
        }

        public async Task<IActionResult> GetDataSourcesByRelationshipId(HttpRequest request)
        {
            request.Query.TryGetValue("relationshipId", out Microsoft.Extensions.Primitives.StringValues relId);

            string relationshipId = relId.FirstOrDefault();

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

            IEnumerable<Dataset> datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Definition.Id == relationship.DatasetDefinition.Id);

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
                            Versions = dataset.History.Select(m => m.Version)
                        }
                });
                }
            }
            return new OkObjectResult(selectDatasourceModel);
        }

        public async Task<IActionResult> AssignDatasourceVersionToRelationship(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            AssignDatasourceModel model = JsonConvert.DeserializeObject<AssignDatasourceModel>(json);

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

            return new NoContentResult();
        }

        public async Task<IActionResult> GetRelationshipBySpecificationIdAndName(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out Microsoft.Extensions.Primitives.StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("The specification id was not provided to GetRelationshipsBySpecificationIdAndName");

                return new BadRequestObjectResult("No specification id was provided to GetRelationshipsBySpecificationIdAndName");
            }

            request.Query.TryGetValue("name", out Microsoft.Extensions.Primitives.StringValues name);

            string relationshipName = name.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(relationshipName))
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

        public async Task<IActionResult> GetCurrentRelationshipsBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out Microsoft.Extensions.Primitives.StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"No specification id was provided to GetCurrentRelationshipsBySpecificationId");

                return new BadRequestObjectResult($"Null or empty {nameof(specificationId)} provided");
            }

            SpecificationSummary specification = await _specificationsRepository.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                _logger.Error($"Failed to find specification for id: {specificationId}");

                return new StatusCodeResult(412);
            }

            IEnumerable<DefinitionSpecificationRelationship> relationships =
               (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Specification.Id == specificationId)).ToList();

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

            foreach (DefinitionSpecificationRelationship relationship in relationships)
            {
                Task<DatasetSpecificationRelationshipViewModel> task = CreateViewModel(relationship);

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
               (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Specification.Id == specificationId)).ToList();

            if (relationships.IsNullOrEmpty())
            {
                relationships = new DefinitionSpecificationRelationship[0];
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

        private async Task<DatasetSpecificationRelationshipViewModel> CreateViewModel(DefinitionSpecificationRelationship relationship)
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
