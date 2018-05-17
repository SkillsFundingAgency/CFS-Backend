using CalculateFunding.Models.Calcs;
using FluentValidation;

namespace CalculateFunding.Services.Calcs.Validators
{
    public class PreviewRequestModelValidator : AbstractValidator<PreviewRequest>
    {
        public PreviewRequestModelValidator()
        {
            RuleFor(preview => preview.CalculationId)
              .NotEmpty()
              .WithMessage("Null or empty calculation id provided");

            RuleFor(preview => preview.SourceCode)
              .NotEmpty()
              .WithMessage("Null or empty calculation id provided");

            RuleFor(preview => preview.SpecificationId)
              .NotEmpty()
              .WithMessage("Null or empty specification id provided");
        }
    }
}
