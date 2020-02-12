using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using PublishingProvider = CalculateFunding.Models.Publishing.Provider;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    /// <summary>
    /// Detects changes in provider metadata between the current saved and the current state of the provider.
    /// Add a variation reason for the relevant field if it has changed.
    /// </summary>
    public class ProviderMetadataVariationStrategy : IVariationStrategy
    {
        private class VariationCheck
        {
            private static readonly PropertyInfo[] UpdatedStateProperties 
                = typeof(ApiProvider).GetProperties();
        
            private readonly VariationReason _variationReason;
            private readonly PropertyInfo _priorStateAccessor;
            private readonly PropertyInfo _updatedStateAccessor;
            
            public VariationCheck(PropertyInfo priorStateAccessor)
            {
                _variationReason = ((VariationReasonValueAttribute)priorStateAccessor
                    .GetCustomAttributes(typeof(VariationReasonValueAttribute)).First())
                    .Value;
                _priorStateAccessor = priorStateAccessor;
                _updatedStateAccessor = UpdatedStateProperties.FirstOrDefault(_ => _.Name == priorStateAccessor.Name);
                
                Guard.ArgumentNotNull(_updatedStateAccessor, nameof(priorStateAccessor.Name));
            }

            public void Run(ProviderVariationContext providerVariationContext)
            {
                string priorValue = (string)_priorStateAccessor.GetValue(providerVariationContext.PriorState.Provider);
                string currentValue = (string)_updatedStateAccessor.GetValue(providerVariationContext.UpdatedProvider);

                if (priorValue != currentValue)
                {
                    providerVariationContext.Result.VariationReasons.Add(_variationReason);
                }        
            }
        }

        static ProviderMetadataVariationStrategy()
        {
            VariationChecks = typeof(PublishingProvider)
                .GetProperties()
                .Where(_ => _.GetCustomAttributes(typeof(VariationReasonValueAttribute)).Any())
                .Select(_ => new VariationCheck(_))
                .ToArray();
        }

        private static readonly VariationCheck[] VariationChecks;

        public string Name => "ProviderMetadata";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext)
        {
            if (providerVariationContext.PriorState == null || providerVariationContext.UpdatedProvider == null)
            {
                return Task.CompletedTask;
            }

            foreach (VariationCheck variationCheck in VariationChecks)
            {
                variationCheck.Run(providerVariationContext);
            }

            if (providerVariationContext.Result.VariationReasons.AnyWithNullCheck())
            {
                providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext));
            }

            return Task.CompletedTask;
        }
    }
}
