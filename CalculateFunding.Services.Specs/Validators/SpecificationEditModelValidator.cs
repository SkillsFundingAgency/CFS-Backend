using CalculateFunding.Common.ApiClient.Policies;
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
        private readonly IPoliciesApiClient _policiesApiClient;

        public SpecificationEditModelValidator(
            ISpecificationsRepository specificationsRepository, 
            IProvidersApiClient providersApiClient,
            IPoliciesApiClient policiesApiClient
            )
        {
            
            _specificationsRepository = specificationsRepository;
            _providersApiClient = providersApiClient;
            _policiesApiClient = policiesApiClient;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the specification");

            RuleFor(model => model.FundingPeriodId)
                .NotEmpty()
                .WithMessage("Null or empty funding period id")
                .Custom((name, context) => {
                    SpecificationEditModel specModel = context.ParentContext.InstanceToValidate as SpecificationEditModel;
                    if (_policiesApiClient.GetFundingPeriodById(specModel.FundingPeriodId).Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        context.AddFailure($"Funding period id selected does not exist");
                    }
                });

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

                   Specification specification = _specificationsRepository.GetSpecificationByQuery(m => m.Name.ToLower() == specModel.Name.Trim().ToLower() && m.Id != specModel.SpecificationId).Result;
                   if (specification != null)
                       context.AddFailure($"You must give a unique specification name");
               });
        }
    }
}
