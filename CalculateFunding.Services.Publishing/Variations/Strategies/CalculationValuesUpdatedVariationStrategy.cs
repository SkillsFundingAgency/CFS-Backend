﻿using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class CalculationValuesUpdatedVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "CalculationValuesUpdated";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext?.PriorState;
            PublishedProviderVersion refreshState = providerVariationContext?.RefreshState;

            bool calculationValuesUpdated = false;

            if (priorState?.Calculations == null || refreshState?.Calculations == null)
            {
                return Task.FromResult(false);
            }

            foreach (FundingCalculation priorFundingCalculation in priorState.Calculations)
            {
                FundingCalculation refreshFundingCalculation = refreshState.Calculations.SingleOrDefault(_ => _.TemplateCalculationId == priorFundingCalculation.TemplateCalculationId);

                if (refreshFundingCalculation != null)
                {
                    if (priorFundingCalculation.Value != refreshFundingCalculation.Value)
                    {
                        calculationValuesUpdated = true;
                    }
                }
            }

            if (!calculationValuesUpdated)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.AddVariationReasons(VariationReason.CalculationValuesUpdated);

            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext, Name));

            return Task.FromResult(false);
        }
    }
}
