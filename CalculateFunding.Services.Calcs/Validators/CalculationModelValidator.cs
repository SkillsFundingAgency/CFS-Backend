using CalculateFunding.Models.Calcs;
using FluentValidation;

namespace CalculateFunding.Services.Calcs.Validators
{
    public class CalculationModelValidator : AbstractValidator<Calculation>
    {
        public CalculationModelValidator()
        {
            RuleFor(calc => calc.Id)
              .NotEmpty()
              .WithMessage("Null or empty calculation id provided");

            RuleFor(calc => calc.Name)
              .NotEmpty()
              .WithMessage("Null or empty calculation name provided");

            RuleFor(calc => calc.SpecificationId)
               .NotEmpty()
               .NotNull()
               .WithMessage("invalid specification ID provided");
        }
    }
}
