using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
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
               .WithMessage("Missing dataset definition id.")
               .CustomAsync(async (name, context, ct) =>
               {
                   CreateDefinitionSpecificationRelationshipModel relationshipModel = context.ParentContext.InstanceToValidate as CreateDefinitionSpecificationRelationshipModel;
                   if (relationshipModel.ConverterEnabled && !string.IsNullOrWhiteSpace(relationshipModel.DatasetDefinitionId))
                   {
                       DatasetDefinition datasetDefinition = await _datasetRepository.GetDatasetDefinition(relationshipModel.DatasetDefinitionId);

                       if (!datasetDefinition.ConverterEligible)
                       {
                           context.AddFailure("You cannot enable the relationship as converter enabled as the dataset definition does not allow it");
                       }
                   }
               });

            RuleFor(model => model.ConverterEnabled)
                .CustomAsync(async (name, context, ct) =>
                {
                    CreateDefinitionSpecificationRelationshipModel relationshipModel = context.ParentContext.InstanceToValidate as CreateDefinitionSpecificationRelationshipModel;
                    if (relationshipModel.ConverterEnabled && !string.IsNullOrWhiteSpace(relationshipModel.DatasetDefinitionId))
                    {
                        DatasetDefinition datasetDefinition = await _datasetRepository.GetDatasetDefinition(relationshipModel.DatasetDefinitionId);

                        if (!datasetDefinition.ConverterEligible)
                        {
                            context.AddFailure("You cannot enable the relationship as converter enabled as the dataset definition does not allow it");
                        }
                    }
                });

            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .WithMessage("Missing specification id.");

            RuleFor(model => model.Name)
              .CustomAsync(async(name, context, ct) => {
                  CreateDefinitionSpecificationRelationshipModel relationshipModel = context.ParentContext.InstanceToValidate as CreateDefinitionSpecificationRelationshipModel;
                  if (string.IsNullOrWhiteSpace(relationshipModel.Name))
                  {
                      context.AddFailure("Missing name provided");
                  }
                  else
                  {
                      if (!string.IsNullOrWhiteSpace(relationshipModel.SpecificationId))
                      {
                          DefinitionSpecificationRelationship relationship = await _datasetRepository.GetRelationshipBySpecificationIdAndName(relationshipModel.SpecificationId, relationshipModel.Name);

                          if (relationship != null)
                              context.AddFailure("You must give a unique relationship name");
                      }
                  }
              });

            RuleFor(model => model.Description)
              .NotEmpty()
              .WithMessage("Missing description provided.");
            
        }
    }
}
