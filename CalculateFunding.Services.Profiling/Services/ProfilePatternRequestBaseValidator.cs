using System;
using System.Linq;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Services.Profiling.Models;
using FluentValidation;

namespace CalculateFunding.Services.Profiling.Services
{
    public abstract class ProfilePatternRequestBaseValidator<TRequest> : AbstractValidator<TRequest>
        where TRequest : ProfilePatternRequestBase
    {
        protected ProfilePatternRequestBaseValidator()
        {
            RuleFor(_ => _.Pattern.FundingPeriodId)
                .NotEmpty()
                .WithMessage("You must provide a funding period id")
                .When(_ => _.Pattern != null);

            RuleFor(_ => _.Pattern.FundingStreamId)
                .NotEmpty()
                .WithMessage("You must provide a funding stream id")
                .When(_ => _.Pattern != null);

            RuleFor(_ => _.Pattern.FundingLineId)
                .NotEmpty()
                .WithMessage("You must provide a funding line id")
                .When(_ => _.Pattern != null);

            RuleFor(_ => _.Pattern.FundingStreamPeriodStartDate)
                .GreaterThan(DateTime.MinValue)
                .WithMessage("You must provide a funding stream period start date")
                .When(_ => _.Pattern != null);

            RuleFor(_ => _.Pattern.FundingStreamPeriodEndDate)
                .GreaterThan(_ => _.Pattern.FundingStreamPeriodStartDate)
                .WithMessage("Funding stream period end date must after funding stream period start date")
                .When(_ => _.Pattern.FundingStreamPeriodStartDate > DateTime.MinValue)
                .When(_ => _.Pattern != null);

            RuleFor(_ => _.Pattern.ProfilePatternDisplayName)
                .NotEmpty()
                .WithMessage("Null or Empty profile pattern display name provided")
                .When(_ => _.Pattern != null);

            RuleFor(_ => _.Pattern)
                .Custom((pattern, ctx) =>
                {
                    if (pattern == null)
                    {
                        return;
                    }

                    ProfilePeriodPattern[] patterns = pattern.ProfilePattern;

                    if (patterns.IsNullOrEmpty())
                    {
                        ctx.AddFailure(nameof(FundingStreamPeriodProfilePattern.ProfilePattern),
                            "The profile pattern must have at least one period");

                        return;
                    }

                    if (patterns.GroupBy(_ => new
                    {
                        _.PeriodYear,
                        _.PeriodType,
                        _.Period,
                        _.Occurrence
                    }).Any(_ => _.Count() > 1))
                    {
                        ctx.AddFailure(nameof(FundingStreamPeriodProfilePattern.ProfilePattern),
                            "The profile periods must be for unique dates and occurence");
                    }

                    if (Math.Round(patterns.Sum(_ => _.PeriodPatternPercentage), 3) != 100M)
                    {
                        ctx.AddFailure(nameof(FundingStreamPeriodProfilePattern.ProfilePattern),
                            "The profile period percentages must total 100%");
                    }
                });

            RuleFor(_ => _.Pattern)
                .Custom((pattern, ctx) =>
                {
                    if(pattern == null)
                    {
                        return;
                    }

                    if(string.IsNullOrWhiteSpace(pattern.ProfilePatternKey) && 
                     !pattern.ProviderTypeSubTypes.IsNullOrEmpty())
                    {
                        ctx.AddFailure(nameof(FundingStreamPeriodProfilePattern.ProfilePatternKey), "Default pattern not allowed to have ProviderTypeSubTypes");
                    }
                });
        }
    }
}