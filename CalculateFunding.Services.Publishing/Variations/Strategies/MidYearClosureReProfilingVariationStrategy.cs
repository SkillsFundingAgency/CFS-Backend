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
    public class MidYearClosureReProfilingVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "MidYearClosureReProfiling";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext,
            IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;

            IEnumerable<string> fundingLinesWithReleasedAllocations = FundingLinesWithReleasedAllocations(priorState);

            if (VariationPointersNotSet(providerVariationContext) ||
                priorState?.Provider.Status == Closed ||
                providerVariationContext.UpdatedProvider.Status != Closed ||
                fundingLinesWithReleasedAllocations.IsNullOrEmpty())
            {
                return Task.FromResult(false);
            }

            fundingLinesWithReleasedAllocations.ForEach(_ => providerVariationContext.AddAffectedFundingLineCode(Name, _));

            return Task.FromResult(true);
        }

        private IEnumerable<string> FundingLinesWithReleasedAllocations(PublishedProviderVersion priorState)
        {
            if (priorState == null)
            {
                return null;
            }

            List<string> fundingLines = new List<string>(); ;

            foreach (FundingLine latestFundingLine in priorState.PaymentFundingLinesWithValues.Where(_ => _.Value != 0))
            {
                fundingLines.Add(latestFundingLine.FundingLineCode);
            }

            return fundingLines;
        }

        private static bool VariationPointersNotSet(ProviderVariationContext providerVariationContext) => !(providerVariationContext.VariationPointers?.Any()).GetValueOrDefault();

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.QueueVariationChange(new MidYearClosureReProfileVariationChange(providerVariationContext, Name));

            // Stop subsequent strategies                    
            return Task.FromResult(true);
        }
    }
}