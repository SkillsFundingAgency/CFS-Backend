using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using ResiliencePolicy = Polly.AsyncPolicy;

namespace CalculateFunding.Services.Providers.Validators
{
    public class SaveFundingConfigurationValidator : AbstractValidator<FundingConfiguration>
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly ResiliencePolicy _policyRepositoryPolicy;
        private readonly IFundingTemplateService _fundingTemplateService;

        public SaveFundingConfigurationValidator(IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IFundingTemplateService fundingTemplateService)
        {
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(fundingTemplateService, nameof(fundingTemplateService));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));

            _policyRepository = policyRepository;
            _fundingTemplateService = fundingTemplateService;
            _policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;

            RuleFor(model => model.FundingStreamId)
                .NotEmpty()
                .WithMessage("No funding stream id was provided to SaveFundingConfiguration")
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    FundingConfiguration model = context.ParentContext.InstanceToValidate as FundingConfiguration;
                    if (!string.IsNullOrWhiteSpace(model.FundingStreamId))
                    {
                        FundingStream fundingStream = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingStreamById(model.FundingStreamId));
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
                        FundingPeriod fundingPeriod = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingPeriodById(model.FundingPeriodId));
                        if (fundingPeriod == null) context.AddFailure("Funding period not found");
                    }
                });

            RuleFor(model => model.DefaultTemplateVersion)
                .NotEmpty()
                .WithMessage("No default template version was provided to SaveFundingConfiguration")
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    FundingConfiguration model = context.ParentContext.InstanceToValidate as FundingConfiguration;

                    string fundingStreamId = model.FundingStreamId;
                    string defaultTemplateVersion = model.DefaultTemplateVersion;

                    if (!string.IsNullOrWhiteSpace(fundingStreamId) && !string.IsNullOrWhiteSpace(defaultTemplateVersion))
                        if (!await _fundingTemplateService.TemplateExists(fundingStreamId, defaultTemplateVersion))
                            context.AddFailure("Default template not found");
                });
        }
    }
}