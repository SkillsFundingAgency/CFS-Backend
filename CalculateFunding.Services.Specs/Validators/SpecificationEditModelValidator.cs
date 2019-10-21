using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Specs.Validators
{
    public class SpecificationEditModelValidator : AbstractValidator<SpecificationEditModel>
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IProvidersApiClient _providersApiClient;

        public SpecificationEditModelValidator(ISpecificationsRepository specificationsRepository, IProvidersApiClient providersApiClient)
        {
            _specificationsRepository = specificationsRepository;
            _providersApiClient = providersApiClient;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the specification");

            RuleFor(model => model.FundingPeriodId)
               .NotEmpty()
               .WithMessage("Null or empty academic year id provided");

            RuleFor(model => model.ProviderVersionId)
                .NotEmpty()
                .WithMessage("Null or empty provider version id")
                .Custom((name, context) => {
                    SpecificationEditModel specModel = context.ParentContext.InstanceToValidate as SpecificationEditModel;
                    if (_providersApiClient.DoesProviderVersionExist(specModel.ProviderVersionId).Result == System.Net.HttpStatusCode.NotFound)
                    {
                        context.AddFailure($"Provider version id selected does not exist");
                    }
                });

            RuleFor(model => model.FundingStreamIds)
              .NotNull()
              .NotEmpty()
              .WithMessage("You must select at least one funding stream")
              .Custom((name, context) => {
                  SpecificationEditModel specModel = context.ParentContext.InstanceToValidate as SpecificationEditModel;
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
                   SpecificationEditModel specModel = context.ParentContext.InstanceToValidate as SpecificationEditModel;

                   if (string.IsNullOrWhiteSpace(specModel.SpecificationId))
                   {
                       context.AddFailure("Specification ID not specified on the model");
                       return;
                   }

                   Specification specification = _specificationsRepository.GetSpecificationByQuery(m => m.Name.Trim().ToLower() == specModel.Name.ToLower() && m.Id != specModel.SpecificationId).Result;
                   if (specification != null)
                       context.AddFailure($"You must give a unique specification name");
               });
        }
    }
}
