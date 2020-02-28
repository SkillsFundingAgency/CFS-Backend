using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class NewOpenerVariationStrategy : Variation, IVariationStrategy
    {
        public string Name => "NewOpener";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));
            
            if (providerVariationContext.PriorState.Provider.Status == Opened ||
                providerVariationContext.UpdatedProvider.Status != Opened)
            {
                return Task.CompletedTask;
            }

            providerVariationContext.QueueVariationChange(new ZeroAllProfiles(providerVariationContext));

            return Task.CompletedTask;
        }
    }
}
