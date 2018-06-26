using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Specs.Validators
{
    public class PolicyEditModelValidator : AbstractValidator<PolicyEditModel>
    {
        readonly ISpecificationsRepository _specificationsRepository;

        public PolicyEditModelValidator(ISpecificationsRepository specificationsRepository)
        {
            _specificationsRepository = specificationsRepository;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the policy");

            RuleFor(model => model.SpecificationId)
               .NotEmpty()
               .WithMessage("Null or empty specification id provided");

            RuleFor(model => model.ParentPolicyId)
              .Custom((name, context) =>
              {
                  PolicyEditModel model = context.ParentContext.InstanceToValidate as PolicyEditModel;
                  if (!string.IsNullOrEmpty(model.ParentPolicyId))
                  {
                      Policy policy = _specificationsRepository.GetPolicyBySpecificationIdAndPolicyId(model.SpecificationId, model.ParentPolicyId).Result;

                      if (policy == null)
                          context.AddFailure($"The parent policy does not exist");
                  }
              });

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage("You must give a unique policy name")
               .Custom((name, context) => {
                   PolicyEditModel model = context.ParentContext.InstanceToValidate as PolicyEditModel;

                   if (string.IsNullOrWhiteSpace(model.PolicyId))
                   {
                       context.AddFailure("Policy ID not specified on the model");
                       return;
                   }

                   if (!string.IsNullOrEmpty(model.SpecificationId))
                   {
                        Policy policy = _specificationsRepository.GetPolicyBySpecificationIdAndPolicyName(model.SpecificationId, model.Name).Result;
                      
                        if (policy != null && policy.Id != model.PolicyId)
                            context.AddFailure($"You must give a unique policy name");
                   }
               });
        }
    }
}
