using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
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

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionSpecificationRelationshipService : IDefinitionSpecificationRelationshipService
    {
        const string updateSpecificationSearchIndex = "spec-events-add-definition-relationship";

        private readonly IDatasetRepository _datasetRepository;
        private readonly ILogger _logger;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IValidator<CreateDefinitionSpecificationRelationshipModel> _relationshipModelValidator;
        private readonly IMessengerService _messengerService;
        private readonly ServiceBusSettings _serviceBusSettings;

        public DefinitionSpecificationRelationshipService(IDatasetRepository datasetRepository, 
            ILogger logger, ISpecificationsRepository specificationsRepository, 
            IValidator<CreateDefinitionSpecificationRelationshipModel> relationshipModelValidator, 
            IMessengerService messengerService, ServiceBusSettings serviceBusSettings)
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
            _serviceBusSettings = serviceBusSettings;
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
                Id = relationshipId
            };

            HttpStatusCode statusCode = await _datasetRepository.SaveDefinitionSpecificationRelationship(relationship);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save relationship with status code: {statusCode.ToString()}");
                return new StatusCodeResult((int)statusCode);
            }

            IDictionary<string, string> properties = CreateMessageProperties(request);

            await _messengerService.SendAsync(_serviceBusSettings.SpecsServiceBusTopicName, updateSpecificationSearchIndex,
                new AssignDefinitionRelationshipMessage
                {
                   SpecificationId = specification.Id,
                   RelationshipId = relationshipId
                },
                properties);

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
