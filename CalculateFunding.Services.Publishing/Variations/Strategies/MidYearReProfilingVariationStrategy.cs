using System;
using System.Collections;
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
                VariationPointersNotSet(providerVariationContext))
            {
                return Task.FromResult(false);
            }

            IEnumerable<string> newOpenerFundingLines = NewOpenerFundingLines(priorState, refreshState);
            IEnumerable<string> fundingLinesWithNewAllocations = FundingLinesWithNewAllocations(priorState, refreshState);
            IEnumerable<string> indicativeAffectedFundingLines = providerVariationContext.AffectedFundingLineCodes("IndicativeToLive");

            if (newOpenerFundingLines.IsNullOrEmpty() &&
                fundingLinesWithNewAllocations.IsNullOrEmpty() &&
                indicativeAffectedFundingLines.IsNullOrEmpty())
            {
                return Task.FromResult(false);
            }

            Concatenate(newOpenerFundingLines ?? ArraySegment<string>.Empty,
                fundingLinesWithNewAllocations ?? ArraySegment<string>.Empty,
                indicativeAffectedFundingLines ?? ArraySegment<string>.Empty)
                .ForEach(_ => providerVariationContext.AddAffectedFundingLineCode(Name, _));

            return Task.FromResult(true);
        }

        private static IEnumerable<T> Concatenate<T>(params IEnumerable<T>[] lists)
        {
            return lists.SelectMany(_ => _);
        }

        private IEnumerable<string> NewOpenerFundingLines(PublishedProviderVersion priorState,
            PublishedProviderVersion refreshState)
        {
            if (priorState != null)
            {
                return null;
            }

            List<string> fundingLines = new List<string>();

            // we only need to re-profile an opener if it has a none zero value
            foreach (FundingLine fundingLine in refreshState.PaymentFundingLinesWithValues.Where(_ => _.Value != 0))  
            {
                fundingLines.Add(fundingLine.FundingLineCode);
            }

            return fundingLines;
        }

        private IEnumerable<string> FundingLinesWithNewAllocations(PublishedProviderVersion priorState,
            PublishedProviderVersion refreshState)
        {
            if (priorState == null)
            {
                return null;
            }
            
            HashSet<string> priorFundingLineCodes = PaymentFundingLineWithValues(priorState);

            List<string> fundingLines = new List<string>();

            foreach (FundingLine latestFundingLine in NewAllocations(refreshState, priorFundingLineCodes))
            {
                fundingLines.Add(latestFundingLine.FundingLineCode);
            }

            return fundingLines;
        }

        private static IEnumerable<FundingLine> NewAllocations(PublishedProviderVersion refreshState,
            HashSet<string> priorFundingLineCodes) =>
            // this is an opener or newly funded variation strategy therefore
            // only apply variation to funding lines which have a none 0 value for current refresh state
            refreshState.PaymentFundingLinesWithValues.Where(_ => _.Value != 0 && !priorFundingLineCodes.Contains(_.FundingLineCode));

        private HashSet<string> PaymentFundingLineWithValues(PublishedProviderVersion publishedProviderVersion)
            => publishedProviderVersion.PaymentFundingLinesWithValues?.Where(_ => _.DistributionPeriods != null).Select(_ => _.FundingLineCode).ToHashSet() ?? new HashSet<string>();

        private static bool VariationPointersNotSet(ProviderVariationContext providerVariationContext) => !(providerVariationContext.VariationPointers?.Any()).GetValueOrDefault();

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.QueueVariationChange(new MidYearReProfileVariationChange(providerVariationContext, Name));
            
            return Task.FromResult(true);
        }
    }
}