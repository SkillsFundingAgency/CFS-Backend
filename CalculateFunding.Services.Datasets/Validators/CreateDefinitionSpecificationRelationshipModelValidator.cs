using CalculateFunding.Models.Datasets;
using FluentValidation;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class CreateDefinitionSpecificationRelationshipModelValidator : AbstractValidator<CreateDefinitionSpecificationRelationshipModel>
    {
        public CreateDefinitionSpecificationRelationshipModelValidator()
        {
            RuleFor(model => model.DatasetDefinitionId)
               .NotEmpty()
               .WithMessage("Missing dataset definition id.");

            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .WithMessage("Missing specification id.");

            RuleFor(model => model.Name)
              .NotEmpty()
              .WithMessage("Missing name provided.");

            RuleFor(model => model.Description)
              .NotEmpty()
              .WithMessage("Missing description provided.");
        }
    }
}
