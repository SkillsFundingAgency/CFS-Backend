using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class CreateDefinitionSpecificationRelationshipModelValidator : AbstractValidator<CreateDefinitionSpecificationRelationshipModel>
    {
        private readonly IDatasetRepository _datasetRepository;

        public CreateDefinitionSpecificationRelationshipModelValidator(IDatasetRepository datasetRepository)
        {
            _datasetRepository = datasetRepository;

            RuleFor(model => model.DatasetDefinitionId)
               .NotEmpty()
               .WithMessage("Missing dataset definition id.");

            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .WithMessage("Missing specification id.");

            RuleFor(model => model.Name)
              .Custom((name, context) => {
                  CreateDefinitionSpecificationRelationshipModel relationshipModel = context.ParentContext.InstanceToValidate as CreateDefinitionSpecificationRelationshipModel;
                  if (string.IsNullOrWhiteSpace(relationshipModel.Name))
                  {
                      context.AddFailure("Missing name provided");
                  }
                  else
                  {
                      if (!string.IsNullOrWhiteSpace(relationshipModel.SpecificationId))
                      {
                          DefinitionSpecificationRelationship relationship = _datasetRepository.GetRelationshipBySpecificationIdAndName(relationshipModel.SpecificationId, relationshipModel.Name).Result;

                          if (relationship != null)
                              context.AddFailure($"You must give a unique relationship name");
                      }
                  }
              });

            RuleFor(model => model.Description)
              .NotEmpty()
              .WithMessage("Missing description provided.");
            
        }
    }
}
