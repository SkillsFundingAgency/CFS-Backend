using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class FundingSchemaUpdatedVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "FundingSchemaUpdated";

        protected override async Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            string priorSchemaVersion = await providerVariationContext.GetPriorStateSchemaVersion();
            string refreshSchemaVersion = await providerVariationContext.GetRefreshStateSchemaVersion();

            if ((string.IsNullOrWhiteSpace(priorSchemaVersion) && string.IsNullOrWhiteSpace(refreshSchemaVersion)) ||
                priorSchemaVersion == refreshSchemaVersion)
            {
                return false;
            }

            return true;
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.AddVariationReasons(VariationReason.FundingSchemaUpdated);
            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext, Name));

            return Task.FromResult(false);
        }
    }
}