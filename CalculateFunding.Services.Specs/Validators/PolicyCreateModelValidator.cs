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
               .NotNull()
               .WithMessage("Null or empty description provided");

            RuleFor(model => model.SpecificationId)
               .NotEmpty()
               .NotNull()
               .WithMessage("Null or empty description provided");

            RuleFor(model => model.Name)
               .NotEmpty()
               .NotNull()
               .WithMessage("Null or empty name provided")
               .Custom((name, context) => {
                   PolicyCreateModel model = context.ParentContext.InstanceToValidate as PolicyCreateModel;
                   Specification specification = _specificationsRepository.GetSpecificationById(model.SpecificationId).Result;
                   Policy policy = specification.GetPolicyByName(name);
                   if (policy != null)
                       context.AddFailure($"A policy for this specification with {name} already exists");
               });
        }
    }
}
