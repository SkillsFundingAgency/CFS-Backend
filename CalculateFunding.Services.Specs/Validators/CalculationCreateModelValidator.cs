using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Specs.Validators
{
    public class CalculationCreateModelValidator : AbstractValidator<CalculationCreateModel>
    {
        readonly ISpecificationsRepository _specificationsRepository;

        public CalculationCreateModelValidator(ISpecificationsRepository specificationsRepository)
        {
            _specificationsRepository = specificationsRepository;

            RuleFor(model => model.Description)
               .NotEmpty()
               .NotNull()
               .WithMessage("Null or empty description provided");

            RuleFor(model => model.SpecificationId)
               .NotEmpty()
               .NotNull()
               .WithMessage("Null or empty description provided");

            RuleFor(model => model.AllocationLineId)
               .NotEmpty()
               .NotNull()
               .WithMessage("Null or empty allocation line provided");

            RuleFor(model => model.PolicyId)
               .NotEmpty()
               .NotNull()
               .WithMessage("Null or empty policy id provided");

            RuleFor(model => model.Name)
               .NotEmpty()
               .NotNull()
               .WithMessage("Null or empty name provided")
               .Custom((name, context) => {
                   CalculationCreateModel model = context.ParentContext.InstanceToValidate as CalculationCreateModel;

                   Calculation calculation = _specificationsRepository.GetCalculationByName(model.SpecificationId, model.Name).Result;

                   if (calculation != null)
                       context.AddFailure($"A calculation for this specification with name {name} already exists");
               });
        }
    }
}
