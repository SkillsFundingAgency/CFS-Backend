using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ProfilingUpdatedVariationStrategy : Variation, IVariationStrategy
    {
        public string Name => "ProfilingUpdated";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;

            if (providerVariationContext.ReleasedState == null ||
                priorState.Provider.Status == Closed ||
                providerVariationContext.UpdatedProvider.Status == Closed ||
                HasNoProfilingChanges(priorState, providerVariationContext.RefreshState))
            {
                return Task.CompletedTask;
            }

            providerVariationContext.AddVariationReasons(VariationReason.ProfilingUpdated);

            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext));

            return Task.CompletedTask;
        }

        private bool HasNoProfilingChanges(PublishedProviderVersion priorState, PublishedProviderVersion refreshState)
        {
            IDictionary<string, FundingLine> latestFundingLines =
                refreshState.FundingLines.Where(_ => _.Type == OrganisationGroupingReason.Payment)
                    .ToDictionary(_ => _.FundingLineCode);

            foreach (FundingLine previousFundingLine in priorState.FundingLines.Where(_ => _.Type == OrganisationGroupingReason.Payment))
            { 
                if (!latestFundingLines.TryGetValue(previousFundingLine.FundingLineCode, out FundingLine latestFundingLine))
                {
                    continue;
                }

                ProfilePeriod[] priorProfiling = new YearMonthOrderedProfilePeriods(previousFundingLine).ToArray();
                ProfilePeriod[] latestProfiling = new YearMonthOrderedProfilePeriods(latestFundingLine).ToArray();

                if (!priorProfiling.Select(AsLiteral)
                    .SequenceEqual(latestProfiling.Select(AsLiteral)))
                {
                    return false;
                }
            }

            return true;
        }

        private string AsLiteral(ProfilePeriod profilePeriod)
        {
            return $"{profilePeriod.Year}{profilePeriod.Type}{profilePeriod.TypeValue}{profilePeriod.Occurrence}{profilePeriod.ProfiledValue}";
        }
    }
}