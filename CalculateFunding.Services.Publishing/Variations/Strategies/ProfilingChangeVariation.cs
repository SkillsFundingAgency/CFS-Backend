using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public abstract class ProfilingChangeVariation : Variation
    {
        protected virtual bool HasNoProfilingChanges(PublishedProviderVersion priorState,
            PublishedProviderVersion refreshState,
            ProviderVariationContext providerVariationContext)
        {
            IDictionary<string, FundingLine> latestFundingLines =
                refreshState.FundingLines.Where(_ => _.Type == FundingLineType.Payment)
                    .ToDictionary(_ => _.FundingLineCode);

            bool hasNoProfilingChanges = true;

            foreach (FundingLine previousFundingLine in priorState.FundingLines.Where(_ => _.Type == FundingLineType.Payment && 
                                                                                           _.Value.HasValue &&
                                                                                           ExtraFundingLinePredicate(refreshState, _)))
            {
                string fundingLineCode = previousFundingLine.FundingLineCode;
                
                if (!latestFundingLines.TryGetValue(fundingLineCode, out FundingLine latestFundingLine))
                {
                    continue;
                }

                ProfilePeriod[] priorProfiling = new YearMonthOrderedProfilePeriods(previousFundingLine).ToArray();
                ProfilePeriod[] latestProfiling = new YearMonthOrderedProfilePeriods(latestFundingLine).ToArray();

                if (!priorProfiling.Select(AsLiteral)
                    .SequenceEqual(latestProfiling.Select(AsLiteral)))
                {
                    providerVariationContext.AddAffectedFundingLineCode(fundingLineCode);
                    
                    hasNoProfilingChanges = false;
                }
            }

            return hasNoProfilingChanges;
        }

        protected virtual bool ExtraFundingLinePredicate(PublishedProviderVersion refreshState,
            FundingLine fundingLine) => true;

        private string AsLiteral(ProfilePeriod profilePeriod)
        {
            return $"{profilePeriod.Year}{profilePeriod.Type}{profilePeriod.TypeValue}{profilePeriod.Occurrence}{profilePeriod.ProfiledValue}";
        }    
    }
}