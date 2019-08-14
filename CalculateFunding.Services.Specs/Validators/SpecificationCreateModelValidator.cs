using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using System.Net;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Services.Specs.Validators
{
    public class SpecificationCreateModelValidator : AbstractValidator<SpecificationCreateModel>
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Polly.Policy _policiesApiClientPolicy;
       

        public SpecificationCreateModelValidator(ISpecificationsRepository specificationsRepository, 
            IProvidersApiClient providersApiClient,
            IPoliciesApiClient policiesApiClient,
            ISpecificationsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _specificationsRepository = specificationsRepository;
            _providersApiClient = providersApiClient;
            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;

            RuleFor(model => model.Description)
               .NotEmpty()
               .WithMessage("You must give a description for the specification");

            RuleFor(model => model.FundingPeriodId)
                .NotEmpty()
                .WithMessage("Null or empty academic year id provided")
                .Custom(async (name, context) =>
                {
                    SpecificationCreateModel specModel = context.ParentContext.InstanceToValidate as SpecificationCreateModel;
                    if (!string.IsNullOrWhiteSpace(specModel.FundingPeriodId))
                    {                       
                        ApiResponse<PolicyModels.Period> fundingPeriodResponse = 
                        await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(specModel.FundingPeriodId));
                        if (fundingPeriodResponse?.StatusCode != HttpStatusCode.OK || fundingPeriodResponse?.Content == null)
                        {
                            context.AddFailure("Funding period not found");
                        }
                    }
                });

            RuleFor(model => model.ProviderVersionId)
                .NotEmpty()
                .WithMessage("Null or empty provider version id")
                .Custom((name, context) => {
                    SpecificationCreateModel specModel = context.ParentContext.InstanceToValidate as SpecificationCreateModel;
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
                  SpecificationCreateModel specModel = context.ParentContext.InstanceToValidate as SpecificationCreateModel;
                  foreach (string fundingStreamId in specModel.FundingStreamIds)
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
