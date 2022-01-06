using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class IndicativeToLiveVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "IndicativeToLive";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext?.PriorState;
            PublishedProviderVersion refreshState = providerVariationContext?.RefreshState;

            IEnumerable<string> newOpenerFundingLines = NewOpenerFundingLines(providerVariationContext, refreshState);

            if (priorState == null ||
                !priorState.IsIndicative ||
                refreshState == null ||
                refreshState.IsIndicative ||
                newOpenerFundingLines.IsNullOrEmpty())
            {
                return Task.FromResult(false);
            }

            newOpenerFundingLines.ForEach(_ => providerVariationContext.AddAffectedFundingLineCode(Name, _));

            return Task.FromResult(true);
        }

        private IEnumerable<string> NewOpenerFundingLines(ProviderVariationContext providerVariationContext,
            PublishedProviderVersion refreshState)
        {
            List<string> fundingLines = new List<string>();

            // we only need to re-profile an opener if it has a none zero value
            foreach (FundingLine fundingLine in refreshState.PaymentFundingLinesWithValues.Where(_ => _.Value != 0))
            {
                fundingLines.Add(fundingLine.FundingLineCode);
            }

            return fundingLines;
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.AddVariationReasons(VariationReason.IndicativeToLive);
            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext, Name));
            providerVariationContext.QueueVariationChange(new MidYearReProfileVariationChange(providerVariationContext, Name));

            return Task.FromResult(true);
        }
    }
}
