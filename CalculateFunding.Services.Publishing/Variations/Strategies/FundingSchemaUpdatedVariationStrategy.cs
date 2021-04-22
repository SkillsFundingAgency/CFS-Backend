using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class FundingSchemaUpdatedVariationStrategy : IVariationStrategy
    {
        public string Name => "FundingSchemaUpdated";

        public async Task DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            string priorSchemaVersion = await providerVariationContext.GetPriorStateSchemaVersion();
            string updatedSchemaVersion = await providerVariationContext.GetReleasedStateSchemaVersion();

            if ((string.IsNullOrWhiteSpace(priorSchemaVersion) && string.IsNullOrWhiteSpace(updatedSchemaVersion)) ||
                priorSchemaVersion == updatedSchemaVersion)
            {
                return;
            }

            providerVariationContext.AddVariationReasons(VariationReason.FundingSchemaUpdated);
            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext));
        }
    }
}