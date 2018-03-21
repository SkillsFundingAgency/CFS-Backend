using CalculateFunding.Models.Scenarios;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Scenarios.Validators
{
    public class CreateNewTestScenarioVersionValidator : AbstractValidator<CreateNewTestScenarioVersion>
    {
        public CreateNewTestScenarioVersionValidator()
        {
            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .WithMessage("Missing specification id.");

            RuleFor(model => model.Name)
              .NotEmpty()
              .WithMessage("Missing name.");

            RuleFor(model => model.Scenario)
              .NotEmpty()
              .WithMessage("Missing test scenario.");
        }
    }
}
