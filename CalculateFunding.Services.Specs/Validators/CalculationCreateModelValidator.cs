using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using System;

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
               .WithMessage("You must give a description for the calculation"); 

            RuleFor(model => model.SpecificationId)
               .NotEmpty()
               .WithMessage("Null or empty specification Id provided");

            RuleFor(model => model.PolicyId)
               .NotEmpty()
               .WithMessage("You must select a policy or a sub policy");

            RuleFor(model => model.CalculationType)
               .Must(c => Enum.TryParse(c, true, out CalculationType calcType))
               .WithMessage("An invalid calc type was provided");

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage("You must give a unique calculation name")
               .Custom((name, context) => {
                   CalculationCreateModel model = context.ParentContext.InstanceToValidate as CalculationCreateModel;

                   Models.Specs.Calculation calculation = _specificationsRepository.GetCalculationBySpecificationIdAndCalculationName(model.SpecificationId, model.Name).Result;

                   if (calculation != null)
                       context.AddFailure($"You must give a unique calculation name");
               });
        }
    }
}
