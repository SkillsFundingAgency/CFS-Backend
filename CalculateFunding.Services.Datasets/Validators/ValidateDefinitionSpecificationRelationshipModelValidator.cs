using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class ValidateDefinitionSpecificationRelationshipModelValidator : AbstractValidator<ValidateDefinitionSpecificationRelationshipModel>
    {
        private readonly IDatasetRepository _datasetRepository;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;
        private ISpecificationsApiClient _specificationsApiClient;

        public ValidateDefinitionSpecificationRelationshipModelValidator(
            IDatasetRepository datasetRepository, 
            ITypeIdentifierGenerator typeIdentifierGenerator,
            ISpecificationsApiClient specificationsApiClient)
        {
            _datasetRepository = datasetRepository;
            _typeIdentifierGenerator = typeIdentifierGenerator;
            _specificationsApiClient = specificationsApiClient;

            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .WithMessage("Missing specification id.");

            RuleFor(model => model.TargetSpecificationId)
              .NotEmpty()
              .WithMessage("Missing target specification id.");

            RuleFor(model => model.Name)
              .CustomAsync(async (name, context, ct) =>
              {
                  ValidateDefinitionSpecificationRelationshipModel relationshipModel = context.ParentContext.InstanceToValidate as ValidateDefinitionSpecificationRelationshipModel;
                  if (string.IsNullOrWhiteSpace(relationshipModel.Name))
                  {
                      context.AddFailure("Missing relationship name.");
                  }
                  else
                  {
                      if (!string.IsNullOrWhiteSpace(relationshipModel.SpecificationId) && !string.IsNullOrWhiteSpace(relationshipModel.TargetSpecificationId))
                      {
                          ApiResponse<SpecificationSummary> apiResponse = await _specificationsApiClient.GetSpecificationSummaryById(relationshipModel.TargetSpecificationId);

                          if(!apiResponse.StatusCode.IsSuccess())
                          {
                              context.AddFailure($"Target specification - {relationshipModel.TargetSpecificationId} not found.");
                          }

                          IEnumerable<DefinitionSpecificationRelationship> relationships = await _datasetRepository.GetDefinitionSpecificationRelationshipsBySpecificationId(relationshipModel.SpecificationId);
                         
                          if(relationships.Any(_ => _.Current.RelationshipType == DatasetRelationshipType.ReleasedData && 
                                                   _.Current.PublishedSpecificationConfiguration.SpecificationId == relationshipModel.TargetSpecificationId))
                          {
                              context.AddFailure($"Target specification - {relationshipModel.TargetSpecificationId} already references in an existing relationship.");
                          }

                          string relationshipNameToCheck = relationshipModel.Name.Trim();
                          string vbIdentifier = _typeIdentifierGenerator.GenerateIdentifier(relationshipModel.Name);

                          if (relationships.Any(r => string.Equals(r.Name.Trim(), relationshipNameToCheck, StringComparison.InvariantCultureIgnoreCase) ||
                                                     string.Equals(_typeIdentifierGenerator.GenerateIdentifier(r.Name), vbIdentifier, StringComparison.InvariantCultureIgnoreCase)))
                          {
                              context.AddFailure("You must give a unique relationship name.");
                          }
                      }
                  }
              });
        }
    }
}
