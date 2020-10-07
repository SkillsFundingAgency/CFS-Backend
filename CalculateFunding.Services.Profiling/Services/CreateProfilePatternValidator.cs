using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using FluentValidation;

namespace CalculateFunding.Services.Profiling.Services
{
    public class CreateProfilePatternValidator : ProfilePatternRequestBaseValidator<CreateProfilePatternRequest>
    {
        public CreateProfilePatternValidator(IProfilePatternRepository profilePatterns,
            IProfilingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(profilePatterns, nameof(profilePatterns));
            Guard.ArgumentNotNull(resiliencePolicies?.ProfilePatternRepository, nameof(resiliencePolicies.ProfilePatternRepository));

            RuleFor(_ => _.Pattern)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .CustomAsync(async (pattern, ctx, ct) =>
                {
                    FundingStreamPeriodProfilePattern existingPattern = await resiliencePolicies.ProfilePatternRepository.ExecuteAsync(() =>
                        profilePatterns.GetProfilePattern(pattern.FundingPeriodId,
                            pattern.FundingStreamId,
                            pattern.FundingLineId,
                            pattern.ProfilePatternKey));

                    if (existingPattern != null)
                    {
                        ctx.AddFailure(nameof(FundingStreamPeriodProfilePattern.Id),
                            $"{pattern.Id} is already in use. Please choose a unique profile pattern id");
                    }
                });
        }
    }
}