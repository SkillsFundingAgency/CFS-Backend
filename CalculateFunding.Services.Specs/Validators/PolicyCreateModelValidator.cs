using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Specs.Validators
{
    public class PolicyCreateModelValidator : AbstractValidator<PolicyCreateModel>
    {
        readonly ISpecificationsRepository _specificationsRepository;

        public PolicyCreateModelValidator(ISpecificationsRepository specificationsRepository)
        {
            _specificationsRepository = specificationsRepository;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the policy");
            RuleFor(model => model.SpecificationId)
               .NotEmpty()
               .WithMessage("Null or empty specification id provided");
            RuleFor(model => model.Name)
               .NotEmpty().WithMessage("You must give a unique policy name")
               .Custom((name, context) => {
                   PolicyCreateModel model = context.ParentContext.InstanceToValidate as PolicyCreateModel;
                   if (!string.IsNullOrEmpty(model.SpecificationId))
                   {
                       Specification specification = _specificationsRepository.GetSpecificationById(model.SpecificationId).Result;
                       if (specification != null)
                       {
                           Policy policy = specification.GetPolicyByName(name);
                           if (policy != null)
                               context.AddFailure($"You must give a unique policy name");
                       }
                   }
               });
        }
    }
}
