using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using System.Net;
using CalculateFunding.Common.Utility;
using System.Linq;

namespace CalculateFunding.Services.Specs.Validators
{
    public class SpecificationCreateModelValidator : AbstractValidator<SpecificationCreateModel>
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Polly.AsyncPolicy _policiesApiClientPolicy;
       
        public SpecificationCreateModelValidator(ISpecificationsRepository specificationsRepository, 
            IProvidersApiClient providersApiClient,
            IPoliciesApiClient policiesApiClient,
            ISpecificationsResiliencePolicies resiliencePolicies)
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
                .WithMessage("Null or empty academic year id provided")
                .Custom((name, context) =>
                {
                    SpecificationCreateModel specModel = context.ParentContext.InstanceToValidate as SpecificationCreateModel;
                    if (!string.IsNullOrWhiteSpace(specModel.FundingPeriodId))
                    {                       
                        ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = 
                        _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(specModel.FundingPeriodId)).GetAwaiter().GetResult();
                        if (fundingPeriodResponse?.StatusCode != HttpStatusCode.OK || fundingPeriodResponse?.Content == null)
                        {
                            context.AddFailure("Funding period not found");
                        }
                    }
                });

            RuleFor(model => model.ProviderVersionId)
                .Custom((name, context) => {
                    SpecificationCreateModel specModel = context.ParentContext.InstanceToValidate as SpecificationCreateModel;
                    ApiResponse<PolicyModels.FundingConfig.FundingConfiguration> fundingConfigResponse =
                        _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingConfiguration(specModel.FundingStreamIds.FirstOrDefault(), specModel.FundingPeriodId)).GetAwaiter().GetResult();

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

                                if(specModel.CoreProviderVersionUpdates != CoreProviderVersionUpdates.Manual)
                                {
                                    context.AddFailure($"CoreProviderVersionUpdates - {specModel.CoreProviderVersionUpdates} is not valid for provider source - {fundingConfigResponse.Content.ProviderSource}");
                                }

                                break;
                            }
                        case ProviderSource.FDZ:
                            {
                                if (specModel.CoreProviderVersionUpdates == CoreProviderVersionUpdates.Manual && !specModel.ProviderSnapshotId.HasValue)
                                {
                                    context.AddFailure("Null or empty provider snapshot id");
                                }

                                break;
                            }
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
                   Specification specification = _specificationsRepository.GetSpecificationByQuery(m => m.Content.Name.ToLower() == specModel.Name.Trim().ToLower()).Result;
                   if (specification != null)
                       context.AddFailure($"You must give a unique specification name");
               });

      }       
    }
}
