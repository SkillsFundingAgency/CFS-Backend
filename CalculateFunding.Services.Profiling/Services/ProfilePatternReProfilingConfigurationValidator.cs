using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using FluentValidation;

namespace CalculateFunding.Services.Profiling.Services
{
    public class ProfilePatternReProfilingConfigurationValidator : AbstractValidator<FundingStreamPeriodProfilePattern>
    {
        public ProfilePatternReProfilingConfigurationValidator(IReProfilingStrategyLocator reProfilingStrategyLocator)
        {
            RuleFor(_ => _.ReProfilingConfiguration)
                .Custom((pattern,
                    context) =>
                {
                    if (!pattern.ReProfilingEnabled)
                    {
                        return;
                    }
                    
                    if (!reProfilingStrategyLocator.HasStrategy(pattern.DecreasedAmountStrategyKey))
                    {
                        context.AddFailure("ReProfilingConfiguration.DecreasedAmountStrategyKey", "No matching strategy exists");
                    }

                    if (!reProfilingStrategyLocator.HasStrategy(pattern.IncreasedAmountStrategyKey))
                    {
                        context.AddFailure("ReProfilingConfiguration.IncreasedAmountStrategyKey", "No matching strategy exists");
                    }

                    if (!reProfilingStrategyLocator.HasStrategy(pattern.SameAmountStrategyKey))
                    {
                        context.AddFailure("ReProfilingConfiguration.SameAmountStrategyKey", "No matching strategy exists");
                    }
                });
        }
    }
}