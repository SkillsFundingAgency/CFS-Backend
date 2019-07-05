using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.FundingPolicy.ViewModels;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Providers.Validators
{
    public class SaveFundingConfigurationValidator : AbstractValidator<FundingConfiguration>
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly Polly.Policy _policyRepositoryPolicy;

        public SaveFundingConfigurationValidator(IPolicyRepository policyRepository, IPolicyResilliencePolicies policyResilliencePolicies)
        {
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResilliencePolicies, nameof(policyResilliencePolicies));

            _policyRepository = policyRepository;
            _policyRepositoryPolicy = policyResilliencePolicies.PolicyRepository;

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
