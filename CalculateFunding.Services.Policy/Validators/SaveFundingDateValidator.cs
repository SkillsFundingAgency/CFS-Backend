using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using System.Linq;
using ResiliencePolicy = Polly.AsyncPolicy;

namespace CalculateFunding.Services.Policy.Validators
{
    public class SaveFundingDateValidator : AbstractValidator<FundingDate>
    {
        public SaveFundingDateValidator(
            IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies
            )
        {
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));

            ResiliencePolicy policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;

            RuleFor(model => model.FundingStreamId)
                .NotEmpty()
                .WithMessage("No funding stream id was provided to SaveFundingDate")
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    FundingDate model = context.ParentContext.InstanceToValidate as FundingDate;
                    if (!string.IsNullOrWhiteSpace(model.FundingStreamId))
                    {
                        FundingStream fundingStream = await policyRepositoryPolicy.ExecuteAsync(() => policyRepository.GetFundingStreamById(model.FundingStreamId));
                        if (fundingStream == null) context.AddFailure("Funding stream not found");
                    }
                });

            RuleFor(model => model.FundingPeriodId)
                .NotEmpty()
                .WithMessage("No funding period id was provided to SaveFundingDate")
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    FundingDate model = context.ParentContext.InstanceToValidate as FundingDate;
                    if (!string.IsNullOrWhiteSpace(model.FundingPeriodId))
                    {
                        FundingPeriod fundingPeriod = await policyRepositoryPolicy.ExecuteAsync(() => policyRepository.GetFundingPeriodById(model.FundingPeriodId));
                        if (fundingPeriod == null) context.AddFailure("Funding period not found");
                    }
                });

            RuleFor(model => model.Patterns)
                .NotEmpty()
                .WithMessage("No funding date pattern was provided to SaveFundingDate")
                .Custom((name, context) =>
                {
                    FundingDate model = context.ParentContext.InstanceToValidate as FundingDate;
                    
                    if(model.Patterns == null)
                    {
                        return;
                    }
                    
                    foreach (FundingDatePattern fundingDatePattern in model.Patterns)
                    {
                        if(fundingDatePattern.Occurrence == default ||
                            string.IsNullOrEmpty(fundingDatePattern.Period) ||
                            fundingDatePattern.PeriodYear == default ||
                            fundingDatePattern.PaymentDate == default)
                        {
                            context.AddFailure("FundingDatePattern information missing");
                        }
                    }

                    if( model.Patterns.GroupBy(x => new { x.Period, x.PeriodYear, x.Occurrence }).Any(_ => _.Count() > 1))
                    {
                        context.AddFailure("Duplicate funding data pattern");
                    }
                });
        }
    }
}
