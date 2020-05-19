using System;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Models.Policy;
using FluentValidation;
using Polly;

namespace CalculateFunding.Services.Policy.Validators
{
    public class TemplateCreateAsCloneCommandValidator : AbstractValidator<TemplateCreateAsCloneCommand>
    {
        public TemplateCreateAsCloneCommandValidator(
            IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies)
        {
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));

            AsyncPolicy policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;
            
            RuleFor(x => x.CloneFromTemplateId).NotNull();
            RuleFor(x => x.Name).Length(3, 200);
            RuleFor(x => x.Description).Length(0, 1000);

            RuleFor(x => x.FundingStreamId)
                .NotEmpty()
                .WithMessage("Missing funding stream id")
                .MustAsync(async (command, propertyValue, context, cancellationToken) =>
                {
                    try
                    {
                        FundingStream fundingStream = await policyRepositoryPolicy.ExecuteAsync(() => policyRepository.GetFundingStreamById(command.FundingStreamId));
                        return fundingStream != null;
                    }
                    catch (Exception e)
                    {
                        return false;
                    }
                })
                .WithMessage("Funding stream id does not exist");

            RuleFor(x => x.FundingPeriodId)
                .NotEmpty()
                .WithMessage("Missing funding period id")
                .MustAsync(async (command, propertyValue, context, cancellationToken) =>
                {
                    try
                    {
                        FundingPeriod fundingPeriod = await policyRepositoryPolicy.ExecuteAsync(() => policyRepository.GetFundingPeriodById(command.FundingPeriodId));
                        return fundingPeriod != null;
                    }
                    catch (Exception e)
                    {
                        return false;
                    }
                })
                .WithMessage("Funding period id does not exist");
        }
    }
}