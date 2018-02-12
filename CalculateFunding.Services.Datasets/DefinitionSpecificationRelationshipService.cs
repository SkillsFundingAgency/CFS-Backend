using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class DefinitionSpecificationRelationshipService : IDefinitionSpecificationRelationshipService
    {
        private readonly IDatasetRepository _datasetRepository;
        private readonly ILogger _logger;
        private readonly IApiClientProxy _apiClient;
        private readonly IValidator<CreateDefinitionSpecificationRelationshipModel> _relationshipModelValidator;

        public DefinitionSpecificationRelationshipService(IDatasetRepository datasetRepository, 
            ILogger logger, IApiClientProxy apiClient, IValidator<CreateDefinitionSpecificationRelationshipModel> relationshipModelValidator)
        {
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(relationshipModelValidator, nameof(relationshipModelValidator));

            _datasetRepository = datasetRepository;
            _logger = logger;
            _apiClient = apiClient;
            _relationshipModelValidator = relationshipModelValidator;
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

            Specification specification = await GetSpecificationById(model.SpecificationId);

            if(specification == null)
            {
                _logger.Error($"Specification was not found for id {model.SpecificationId}");
                return new StatusCodeResult(412);
            }

            DefinitionSpecificationRelationship relationship = new DefinitionSpecificationRelationship
            {
                Name = model.Name,
                DatasetDefinition = new Reference(definition.Id, definition.Name),
                Specification = new Reference(specification.Id, specification.Name),
                Description = model.Description,
                Id = Guid.NewGuid().ToString()
            };

            HttpStatusCode statusCode = await _datasetRepository.SaveDefinitionSpecificationRelationship(relationship);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save relationship with status code: {statusCode.ToString()}");
                return new StatusCodeResult((int)statusCode);
            }

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

        Task<Specification> GetSpecificationById(string specificationId)
        {
            string url = $"specs/specifications?specificationId={specificationId}";

            return _apiClient.GetAsync<Specification>(url);
        }
    }
}
