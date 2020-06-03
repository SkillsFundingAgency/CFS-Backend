using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.Requests;
using CalculateFunding.Services.Providers.Interfaces;
using FluentValidation;
using FluentValidation.Results;

namespace CalculateFunding.Services.Providers.Validators
{
    public class SetFundingStreamCurrentProviderVersionRequestValidator : AbstractValidator<SetFundingStreamCurrentProviderVersionRequest>
    {
        public SetFundingStreamCurrentProviderVersionRequestValidator(IProviderVersionsMetadataRepository providerVersions,
            IPoliciesApiClient policiesApiClient,
            IProvidersResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(providerVersions, nameof(providerVersions));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ProviderVersionMetadataRepository, nameof(resiliencePolicies.ProviderVersionMetadataRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            
            RuleFor(_ => _.FundingStreamId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .CustomAsync(async (fundingStreamId,
                    context,
                    cancellationToken) =>
                {
                    ApiResponse<FundingStream> response = await resiliencePolicies.PoliciesApiClient.ExecuteAsync(() =>
                        policiesApiClient.GetFundingStreamById(fundingStreamId));

                    if (response?.Content == null)
                    {
                        context.AddFailure(new ValidationFailure("FundingStreamId",
                            $"No funding stream located with Id {fundingStreamId}"));
                    }
                });
            
            RuleFor(_ => _.ProviderVersionId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .CustomAsync(async (providerVersionId,
                    context,
                    cancellationToken) =>
                {
                    ProviderVersionMetadata response = await resiliencePolicies.ProviderVersionMetadataRepository.ExecuteAsync(() =>
                        providerVersions.GetProviderVersionMetadata(providerVersionId));

                    if (response == null)
                    {
                        context.AddFailure(new ValidationFailure("ProviderVersionId", 
                            $"No provider version located with Id {providerVersionId}"));
                    }
                });
        }
    }
}