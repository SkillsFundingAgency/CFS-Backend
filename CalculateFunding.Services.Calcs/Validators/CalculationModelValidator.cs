﻿using CalculateFunding.Models.Calcs;
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

            RuleFor(calc => calc.CalculationSpecification)
              .Must(m => m != null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name) )
              .WithMessage("Null calculation specification provided");

            RuleFor(calc => calc.SpecificationId)
               .NotEmpty()
               .NotNull()
               .WithMessage("invalid specification ID provided");

            RuleFor(calc => calc.FundingPeriod)
                .Must(m => m != null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name))
                .WithMessage("Invalid period provided");

            RuleFor(calc => calc.FundingStream)
               .Must(m => m == null || ( m != null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name)))
               .WithMessage("Invalid funding stream provided");
        }
    }
}
