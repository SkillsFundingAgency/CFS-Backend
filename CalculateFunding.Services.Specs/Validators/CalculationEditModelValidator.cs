using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using System;

namespace CalculateFunding.Services.Specs.Validators
{
    public class CalculationEditModelValidator : AbstractValidator<CalculationEditModel>
    {
        readonly ISpecificationsRepository _specificationsRepository;

        public CalculationEditModelValidator(ISpecificationsRepository specificationsRepository)
        {
            _specificationsRepository = specificationsRepository;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the calculation"); 

            RuleFor(model => model.SpecificationId)
               .NotEmpty()
               .WithMessage("Null or empty specification Id provided");

            RuleFor(model => model.PolicyId)
               .NotEmpty()
               .WithMessage("You must select a policy or a sub policy");

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage("You must give a unique calculation name")
               .Custom((name, context) => {
                   CalculationEditModel model = context.ParentContext.InstanceToValidate as CalculationEditModel;

                   if (string.IsNullOrWhiteSpace(model.CalculationId))
                   {
                       context.AddFailure("Calculation ID not specified on the model");
                       return;
                   }

                   Models.Specs.Calculation calculation = _specificationsRepository.GetCalculationBySpecificationIdAndCalculationName(model.SpecificationId, model.Name).Result;

                   if (calculation != null && calculation.Id != model.CalculationId)
                       context.AddFailure($"You must give a unique calculation name");
               });
        }
    }
}
