using CalculateFunding.Models.CosmosDbScaling;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.CosmosDbScaling.Validators
{
    public class ScalingConfigurationUpdateModelValidator : AbstractValidator<ScalingConfigurationUpdateModel>
    {
        public ScalingConfigurationUpdateModelValidator()
        {
            RuleFor(model => model.RepositoryType)
               .NotEmpty()
               .WithMessage("A null or empty RepositoryType was provided");

            RuleFor(model => model.MaxRequestUnits)
               .NotEmpty()
               .WithMessage("A null or empty MaxRequestUnits was provided");
        }
    }
}
