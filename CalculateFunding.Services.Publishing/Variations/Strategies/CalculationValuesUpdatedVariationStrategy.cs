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
    public class CalculationValuesUpdatedVariationStrategy : IVariationStrategy
    {
        public string Name => "CalculationValuesUpdated";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext?.PriorState;
            PublishedProviderVersion refreshState = providerVariationContext?.RefreshState;

            bool calculationValuesUpdated = false;

            if(priorState?.Calculations == null || refreshState?.Calculations == null)
            {
                return Task.CompletedTask;
            }

            foreach (FundingCalculation priorFundingCalculation in priorState.Calculations)
            {
                FundingCalculation refreshFundingCalculation = refreshState.Calculations.SingleOrDefault(_ => _.TemplateCalculationId == priorFundingCalculation.TemplateCalculationId);

                if(refreshFundingCalculation != null)
                {
                    if(priorFundingCalculation.Value != refreshFundingCalculation.Value)
                    {
                        calculationValuesUpdated = true;
                    }
                }
            }

            if (!calculationValuesUpdated)
            {
                return Task.CompletedTask;
            }

            providerVariationContext.AddVariationReasons(VariationReason.CalculationValuesUpdated);

            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext));

            return Task.CompletedTask;
        }
    }
}
