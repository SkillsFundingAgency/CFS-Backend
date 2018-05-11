using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Specs.Validators
{
    public class SpecificationCreateModelValidator : AbstractValidator<SpecificationCreateModel>
    {
        private readonly ISpecificationsRepository _specificationsRepository;

        public SpecificationCreateModelValidator(ISpecificationsRepository specificationsRepository)
        {
            _specificationsRepository = specificationsRepository;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the specification");

            RuleFor(model => model.FundingPeriodId)
               .NotEmpty()
               .WithMessage("Null or empty academic year id provided");

            RuleFor(model => model.FundingStreamIds)
              .NotNull()
              .NotEmpty()
              .WithMessage("You must select at least one funding stream")
              .Custom((name, context) => {
                  SpecificationCreateModel specModel = context.ParentContext.InstanceToValidate as SpecificationCreateModel;
                  foreach(string fundingStreamId in specModel.FundingStreamIds)
                  {
                      if (string.IsNullOrWhiteSpace(fundingStreamId))
                      {
                          context.AddFailure($"A null or empty string funding stream ID was provided");
                      }
                  }
              });

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage("You must give a unique specification name")
               .Custom((name, context) => {
                   SpecificationCreateModel specModel = context.ParentContext.InstanceToValidate as SpecificationCreateModel;
                   Specification specification = _specificationsRepository.GetSpecificationByQuery(m => m.Name.ToLower() == specModel.Name.ToLower()).Result;
                   if (specification != null)
                       context.AddFailure($"You must give a unique specification name");
               });
        }
    }
}
