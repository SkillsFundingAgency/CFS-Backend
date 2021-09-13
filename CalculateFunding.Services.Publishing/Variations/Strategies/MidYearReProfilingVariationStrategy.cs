using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class MidYearReProfilingVariationStrategy : Variation, IVariationStrategy
    {
        public string Name => "MidYearReProfiling";

        public Task<VariationStrategyResult> DetermineVariations(ProviderVariationContext providerVariationContext,
            IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProvider fundingApprovalRecord = providerVariationContext.GetPublishedProviderOriginalSnapShot(providerVariationContext.ProviderId);
            
            PublishedProviderVersion priorCurrent = fundingApprovalRecord?.Current;
            PublishedProviderVersion refreshState = providerVariationContext.RefreshState;

            if (HasNoVariationPointers(providerVariationContext) ||
                IsNotNewOpener(providerVariationContext, priorCurrent, refreshState) && 
                HasNoNewAllocations(providerVariationContext, priorCurrent, refreshState))
            {
                return Task.FromResult(StrategyResult);
            }

            providerVariationContext.QueueVariationChange(new MidYearReProfileVariationChange(providerVariationContext));

            // Stop subsequent strategies                    
            StrategyResult.StopSubsequentStrategies = true;

            return Task.FromResult(StrategyResult);
        }

        private bool IsNotNewOpener(ProviderVariationContext providerVariationContext,
            PublishedProviderVersion priorCurrent,
            PublishedProviderVersion refreshState)
        {
            if (priorCurrent != null)
            {
                return true;
            }

            foreach (FundingLine fundingLine in refreshState.PaymentFundingLinesWithValues)  
            {
                providerVariationContext.AddAffectedFundingLineCode(fundingLine.FundingLineCode);   
            }

            return false;
        }

        private bool HasNoNewAllocations(ProviderVariationContext providerVariationContext,
            PublishedProviderVersion priorCurrent,
            PublishedProviderVersion refreshState)
        {
            if (priorCurrent == null)
            {
                return true;
            }
            
            HashSet<string> priorFundingLineCodes = PaymentFundingLineWithValues(priorCurrent);

            bool doesNotHaveNewAllocations = true;

            foreach (FundingLine latestFundingLine in NewAllocations(refreshState, priorFundingLineCodes))
            {
                providerVariationContext.AddAffectedFundingLineCode(latestFundingLine.FundingLineCode);

                doesNotHaveNewAllocations = false;
            }

            return doesNotHaveNewAllocations;
        }

        private static IEnumerable<FundingLine> NewAllocations(PublishedProviderVersion refreshState,
            HashSet<string> priorFundingLineCodes) =>
            refreshState.PaymentFundingLinesWithValues.Where(_ => !priorFundingLineCodes.Contains(_.FundingLineCode));

        private HashSet<string> PaymentFundingLineWithValues(PublishedProviderVersion publishedProviderVersion)
            => publishedProviderVersion.FundingLines?.Where(_ => _.Type == FundingLineType.Payment && _.Value.HasValue).Select(_ => _.FundingLineCode).ToHashSet() ?? new HashSet<string>();

        private static bool HasNoVariationPointers(ProviderVariationContext providerVariationContext) => !(providerVariationContext.VariationPointers?.Any()).GetValueOrDefault();
    }
}