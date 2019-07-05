using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Providers.Validators
{
    public class SaveFundingConfigurationValidator : AbstractValidator<FundingConfiguration>
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly Polly.Policy _policyRepositoryPolicy;

        public SaveFundingConfigurationValidator(IPolicyRepository policyRepository, IPolicyResiliencePolicies policyResiliencePolicies)
        {
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies, nameof(policyResiliencePolicies));

            _policyRepository = policyRepository;
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
                       if (fundingStream == null)
                       {
                           context.AddFailure("Funding stream not found");
                           return;
                       }
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
                       Period fundingPeriod = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingPeriodById(model.FundingPeriodId));
                       if (fundingPeriod == null)
                       {
                           context.AddFailure("Funding period not found");
                           return;
                       }
                   }
               });
        }
    }
}
