using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using ResiliencePolicy = Polly.AsyncPolicy;

namespace CalculateFunding.Services.Policy.Validators
{
    public class SaveFundingConfigurationValidator : AbstractValidator<FundingConfiguration>
    {
        public SaveFundingConfigurationValidator(IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IFundingTemplateService fundingTemplateService)
        {
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(fundingTemplateService, nameof(fundingTemplateService));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));

            ResiliencePolicy policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;

            RuleFor(_ => _.ApprovalMode)
                .Must(_ => _ != ApprovalMode.Undefined)
                .WithMessage("No valid approval mode was selected");

            RuleFor(model => model.FundingStreamId)
                .NotEmpty()
                .WithMessage("No funding stream id was provided to SaveFundingConfiguration")
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    FundingConfiguration model = context.ParentContext.InstanceToValidate as FundingConfiguration;
                    if (!string.IsNullOrWhiteSpace(model.FundingStreamId))
                    {
                        FundingStream fundingStream = await policyRepositoryPolicy.ExecuteAsync(() => policyRepository.GetFundingStreamById(model.FundingStreamId));
                        if (fundingStream == null) context.AddFailure("Funding stream not found");
                    }
                });

            RuleFor(model => model.FundingPeriodId)
                .NotEmpty()
                .WithMessage("No funding period id was provided to SaveFundingConfiguration")
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    FundingConfiguration model = context.ParentContext.InstanceToValidate as FundingConfiguration;
                    if (!string.IsNullOrWhiteSpace(model.FundingPeriodId))
                    {
                        FundingPeriod fundingPeriod = await policyRepositoryPolicy.ExecuteAsync(() => policyRepository.GetFundingPeriodById(model.FundingPeriodId));
                        if (fundingPeriod == null) context.AddFailure("Funding period not found");
                    }
                });

            RuleFor(model => model.DefaultTemplateVersion)
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    FundingConfiguration model = context.ParentContext.InstanceToValidate as FundingConfiguration;

                    string fundingStreamId = model.FundingStreamId;
                    string defaultTemplateVersion = model.DefaultTemplateVersion;
                    string fundingPeriodId = model.FundingPeriodId;

                    if (!string.IsNullOrWhiteSpace(fundingStreamId) && !string.IsNullOrWhiteSpace(fundingPeriodId) && !string.IsNullOrWhiteSpace(defaultTemplateVersion))
                        if (!await fundingTemplateService.TemplateExists(fundingStreamId, fundingPeriodId, defaultTemplateVersion))
                            context.AddFailure("Default template not found");
                });

            RuleFor(model => model.SpecToSpecChannelCode).NotNull();
            RuleFor(model => model.SpecToSpecChannelCode).NotEmpty();

            RuleFor(_ => _.UpdateCoreProviderVersion)
                .Must(v => v == UpdateCoreProviderVersion.Manual)
                .When(_ => _.ProviderSource != CalculateFunding.Models.Providers.ProviderSource.FDZ, ApplyConditionTo.CurrentValidator)
                .WithMessage(x => $"UpdateCoreProviderVersion - {x.UpdateCoreProviderVersion.ToString()} is not valid for provider source - {x.ProviderSource}");

            RuleFor(model => model.AllowedPublishedFundingStreamsIdsToReference)
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    FundingConfiguration model = context.ParentContext.InstanceToValidate as FundingConfiguration;

                    IEnumerable<string> allowedPublishedFundingStreamsIdsToReference = model.AllowedPublishedFundingStreamsIdsToReference;

                    if(allowedPublishedFundingStreamsIdsToReference.AnyWithNullCheck())
                    {
                        foreach (string fundingStreamId in allowedPublishedFundingStreamsIdsToReference)
                        {
                            if(string.IsNullOrWhiteSpace(fundingStreamId))
                            {
                                context.AddFailure($"Null or empty funding stream id not allowed for {nameof(model.AllowedPublishedFundingStreamsIdsToReference)}");
                            }
                            else
                            {
                                FundingStream fundingStream = await policyRepositoryPolicy.ExecuteAsync(() => policyRepository.GetFundingStreamById(fundingStreamId));
                                if (fundingStream == null) context.AddFailure($"Funding stream {fundingStreamId} not found for {nameof(model.AllowedPublishedFundingStreamsIdsToReference)}");
                            }
                        }
                    }
                });
        }
    }
}