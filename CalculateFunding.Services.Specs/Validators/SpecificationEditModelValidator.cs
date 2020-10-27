using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using CalculateFunding.Common.Utility;
using System.Net;
using System.Linq;

namespace CalculateFunding.Services.Specs.Validators
{
    public class SpecificationEditModelValidator : AbstractValidator<SpecificationEditModel>
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Polly.AsyncPolicy _policiesApiClientPolicy;

        public SpecificationEditModelValidator(
            ISpecificationsRepository specificationsRepository, 
            IProvidersApiClient providersApiClient,
            IPoliciesApiClient policiesApiClient,
            ISpecificationsResiliencePolicies resiliencePolicies
            )
        {
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            
            _specificationsRepository = specificationsRepository;
            _providersApiClient = providersApiClient;
            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;

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
                .Custom(async (name, context) => {
                    SpecificationEditModel specModel = context.ParentContext.InstanceToValidate as SpecificationEditModel;
                    ApiResponse<PolicyModels.FundingConfig.FundingConfiguration> fundingConfigResponse =
                        await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingConfiguration(specModel.FundingStreamId, specModel.FundingPeriodId));

                    if (fundingConfigResponse?.StatusCode != HttpStatusCode.OK || fundingConfigResponse?.Content == null)
                    {
                        context.AddFailure("Funding config not found");
                        return;
                    }

                    switch (fundingConfigResponse.Content.ProviderSource)
                    {
                        case ProviderSource.CFS:
                            {
                                if (string.IsNullOrWhiteSpace(specModel.ProviderVersionId))
                                {
                                    context.AddFailure($"Null or empty provider version id");
                                }

                                if (_providersApiClient.DoesProviderVersionExist(specModel.ProviderVersionId).Result == System.Net.HttpStatusCode.NotFound)
                                {
                                    context.AddFailure($"Provider version id selected does not exist");
                                }

                                break;
                            }
                        case ProviderSource.FDZ:
                            {
                                if (!specModel.ProviderSnapshotId.HasValue)
                                {
                                    context.AddFailure($"Null or empty provider snapshot id");
                                }

                                break;
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

                   Specification specification = _specificationsRepository.GetSpecificationByQuery(m => m.Content.Name.ToLower() == specModel.Name.Trim().ToLower() && m.Id != specModel.SpecificationId).Result;
                   if (specification != null)
                       context.AddFailure($"You must give a unique specification name");
               });
        }
    }
}
