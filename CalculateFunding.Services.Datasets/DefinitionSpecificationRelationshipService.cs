using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Constants;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionSpecificationRelationshipService : IDefinitionSpecificationRelationshipService
    {
        private readonly IDatasetRepository _datasetRepository;
        private readonly ILogger _logger;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IValidator<CreateDefinitionSpecificationRelationshipModel> _relationshipModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly ServiceBusSettings _eventHubSettings;
        private readonly IDatasetService _datasetService;
        private readonly ICalcsRepository _calcsRepository;

        public DefinitionSpecificationRelationshipService(IDatasetRepository datasetRepository, 
            ILogger logger, ISpecificationsRepository specificationsRepository, 
            IValidator<CreateDefinitionSpecificationRelationshipModel> relationshipModelValidator, 
            IMessengerService messengerService, ServiceBusSettings eventHubSettings, IDatasetService datasetService, ICalcsRepository calcsRepository)
        {
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(relationshipModelValidator, nameof(relationshipModelValidator));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));

            _datasetRepository = datasetRepository;
            _logger = logger;
            _specificationsRepository = specificationsRepository;
            _relationshipModelValidator = relationshipModelValidator;
            _messengerService = messengerService;
            _eventHubSettings = eventHubSettings;
            _datasetService = datasetService;
            _calcsRepository = calcsRepository;
        }

        async public Task<IActionResult> CreateRelationship(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CreateDefinitionSpecificationRelationshipModel model = JsonConvert.DeserializeObject<CreateDefinitionSpecificationRelationshipModel>(json);

            if (model == null)
            {
                _logger.Error("Null CreateDefinitionSpecificationRelationshipModel was provided to CreateRelationship");
                return new BadRequestObjectResult("Null CreateDefinitionSpecificationRelationshipModel was provided");
            }

            var validationResult = (await _relationshipModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            Models.Datasets.Schema.DatasetDefinition definition = await _datasetRepository.GetDatasetDefinition(model.DatasetDefinitionId);

            if(definition == null)
            {
                _logger.Error($"Datset definition was not found for id {model.DatasetDefinitionId}");
                return new StatusCodeResult(412);
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(model.SpecificationId);

            if(specification == null)
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

            IDictionary<string, string> properties = CreateMessageProperties(request);

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

            return new OkObjectResult(relationship);
        }

        async public Task<IActionResult> GetRelationshipsBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification id was provided to GetRelationshipsBySpecificationId");

                return new BadRequestObjectResult("No specification id was provided to GetRelationshipsBySpecificationId");
            }

            IEnumerable<DefinitionSpecificationRelationship> relationships =
                await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Specification.Id == specificationId);

            if (relationships.IsNullOrEmpty())
                relationships = new DefinitionSpecificationRelationship[0];

            return new OkObjectResult(relationships);
        }

        public async Task<IActionResult> GetDataSourcesByRelationshipId(HttpRequest request)
        {
            request.Query.TryGetValue("relationshipId", out var relId);

            var relationshipId = relId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                _logger.Error("The relationshipId id was not provided to GetDataSourcesByRelationshipId");

                return new BadRequestObjectResult("No relationship id was provided");
            }

            SelectDatasourceModel selectDatasourceModel = new SelectDatasourceModel();

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(relationshipId);

            if (relationship == null)
                return new StatusCodeResult(412);

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
                    selectDatasourceModel.Datasets = Enumerable.Empty<DatasetVersions>();

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

            if(!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to assign data source to relationship : {model.RelationshipId} with status code {statusCode.ToString()}");

                return new StatusCodeResult((int)statusCode);
            }

            IDictionary<string, string> properties = CreateMessageProperties(request);
            properties.Add("specification-id", relationship.Specification.Id);
            properties.Add("relationship-id", relationship.Id);

            await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.ProcessDataset, dataset, properties);

            return new NoContentResult();
        }

        public async Task<IActionResult> GetRelationshipBySpecificationIdAndName(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("The specification id was not provided to GetRelationshipsBySpecificationIdAndName");

                return new BadRequestObjectResult("No specification id was provided to GetRelationshipsBySpecificationIdAndName");
            }

            request.Query.TryGetValue("name", out var name);

            var relationshipName = name.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(relationshipName))
            {
                _logger.Error("The name was not provided to GetRelationshipsBySpecificationIdAndName");

                return new BadRequestObjectResult("No name was provided to GetRelationshipsBySpecificationIdAndName");
            }

            DefinitionSpecificationRelationship relationship = await _datasetRepository.GetRelationshipBySpecificationIdAndName(specificationId, name);

            if (relationship != null)
                return new OkObjectResult(relationship);

            return new NotFoundResult();
        }

        async public Task<IActionResult> GetCurrentRelationshipsBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"No specification id was provided to GetCurrentRelationshipsBySpecificationId");

                return new BadRequestObjectResult($"Null or empty {nameof(specificationId)} provided");
            }

            Specification specification = await _specificationsRepository.GetSpecificationById(specificationId);

            if (specification == null)
            {
                _logger.Error($"Failed to find specification for id: {specificationId}");

                return new StatusCodeResult(412);
            }

            IEnumerable<DefinitionSpecificationRelationship> relationships =
               (await _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(m => m.Specification.Id == specificationId)).ToList();

            if (relationships.IsNullOrEmpty())
                relationships = new DefinitionSpecificationRelationship[0];

            IList<DatasetSpecificationRelationshipViewModel> relationshipViewModels = new List<DatasetSpecificationRelationshipViewModel>();

            if (relationships.IsNullOrEmpty())
                return new OkObjectResult(relationshipViewModels);

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

        async Task<DatasetSpecificationRelationshipViewModel> CreateViewModel(DefinitionSpecificationRelationship relationship)
        {
            DatasetSpecificationRelationshipViewModel relationshipViewModel = new DatasetSpecificationRelationshipViewModel();
            relationshipViewModel.Id = relationship.Id;
            relationshipViewModel.Name = relationship.Name;

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

        IDictionary<string, string> CreateMessageProperties(HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("sfa-correlationId", request.GetCorrelationId());

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }

    }
}
