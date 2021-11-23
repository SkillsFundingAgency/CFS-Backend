using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Variations.Changes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ConverterReProfilingVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "ConverterReProfiling";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext,
            IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            PublishedProviderVersion refreshState = providerVariationContext.RefreshState;

            if (priorState != null ||
                providerVariationContext.UpdatedProvider.Status == Closed ||
                VariationPointersNotSet(providerVariationContext))
            {
                return Task.FromResult(false);
            }

            IEnumerable<string> inYearConverterFundingLines = InYearConverterFundingLines(providerVariationContext.FundingPeriodStartDate,
                providerVariationContext.FundingPeriodEndDate,
                refreshState);

            if (inYearConverterFundingLines.IsNullOrEmpty())
            {
                return Task.FromResult(false);
            }

            inYearConverterFundingLines.ForEach(_ => providerVariationContext.AddAffectedFundingLineCode(Name, _));

            return Task.FromResult(true);
        }

        private IEnumerable<string> InYearConverterFundingLines(DateTimeOffset fundingPeriodStartDate,
            DateTimeOffset fundingPeriodEndDate,
            PublishedProviderVersion refreshState)
        {
            List<string> fundingLines = new List<string>();

            if (refreshState.Provider.ReasonEstablishmentOpened == AcademyConverter &&
                refreshState.Provider.Predecessors.AnyWithNullCheck() &&
                refreshState.Provider.DateOpened != null &&
                refreshState.Provider.DateOpened.Value >= fundingPeriodStartDate &&
                refreshState.Provider.DateOpened.Value <= fundingPeriodEndDate)
            {
                // we only need to re-profile an opener if it has a none zero value
                foreach (FundingLine fundingLine in refreshState.PaymentFundingLinesWithValues.Where(_ => _.Value != 0))
                {
                    fundingLines.Add(fundingLine.FundingLineCode);
                }
            }

            return fundingLines;
        }

        private static bool VariationPointersNotSet(ProviderVariationContext providerVariationContext) => providerVariationContext.VariationPointers.IsNullOrEmpty();

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.QueueVariationChange(new ConverterReProfileVariationChange(providerVariationContext, Name));

            return Task.FromResult(true);
        }
    }
}
