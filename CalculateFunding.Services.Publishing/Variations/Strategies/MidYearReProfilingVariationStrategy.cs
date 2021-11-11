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
    public class MidYearReProfilingVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "MidYearReProfiling";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext,
            IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            PublishedProviderVersion refreshState = providerVariationContext.RefreshState;

            if (providerVariationContext.UpdatedProvider.Status == Closed ||
                VariationPointersNotSet(providerVariationContext) ||
                IsNotNewOpener(providerVariationContext, priorState, refreshState) && 
                HasNoNewAllocations(providerVariationContext, priorState, refreshState))
            {
                return Task.FromResult(false);
            }
            
            return Task.FromResult(true);
        }

        private bool IsNotNewOpener(ProviderVariationContext providerVariationContext,
            PublishedProviderVersion priorState,
            PublishedProviderVersion refreshState)
        {
            if (priorState != null)
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
            PublishedProviderVersion priorState,
            PublishedProviderVersion refreshState)
        {
            if (priorState == null)
            {
                return true;
            }
            
            HashSet<string> priorFundingLineCodes = PaymentFundingLineWithValues(priorState);

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
            => publishedProviderVersion.FundingLines?.Where(_ => _.Type == FundingLineType.Payment && _.Value.HasValue && _.DistributionPeriods != null).Select(_ => _.FundingLineCode).ToHashSet() ?? new HashSet<string>();

        private static bool VariationPointersNotSet(ProviderVariationContext providerVariationContext) => !(providerVariationContext.VariationPointers?.Any()).GetValueOrDefault();

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.QueueVariationChange(new MidYearReProfileVariationChange(providerVariationContext));
            
            return Task.FromResult(true);
        }
    }
}