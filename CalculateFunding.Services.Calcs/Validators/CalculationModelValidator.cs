using CalculateFunding.Models.Calcs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

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

            RuleFor(calc => calc.Specification)
               .Must(m => m != null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name))
               .WithMessage("invalid specification provided");

            RuleFor(calc => calc.Period)
                .Must(m => m != null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name))
                .WithMessage("Invalid period provided");

            RuleFor(calc => calc.AllocationLine)
                .Must(m => m != null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name))
                .WithMessage("Invalid allocation line provided");

            RuleFor(calc => calc.Policies)
            .NotEmpty()
            .WithMessage("No policies were propvided");

            RuleFor(calc => calc.FundingStream)
               .Must(m => m != null && !string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name))
               .WithMessage("Invalid funding stream provided");
        }
    }
}
