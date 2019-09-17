using CalculateFunding.Models.CosmosDbScaling;
using FluentValidation;

namespace CalculateFunding.Services.CosmosDbScaling.Validators
{
    public class ScalingConfigurationUpdateModelValidator : AbstractValidator<ScalingConfigurationUpdateModel>
    {
        public ScalingConfigurationUpdateModelValidator()
        {
            RuleFor(model => model.RepositoryType)
                .IsInEnum()
               .WithMessage("A null or empty or invalid RepositoryType was provided");

            RuleFor(model => model.MaxRequestUnits)
               .NotEmpty()
               .WithMessage("A null or empty MaxRequestUnits was provided");
        }
    }
}
