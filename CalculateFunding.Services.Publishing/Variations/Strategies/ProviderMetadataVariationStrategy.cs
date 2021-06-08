using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    /// <summary>
    /// Detects changes in provider metadata between the current saved and the current state of the provider.
    /// Add a variation reason for the relevant field if it has changed.
    /// </summary>
    public class ProviderMetadataVariationStrategy : Variation, IVariationStrategy
    {
        private class VariationCheck
        {
            private static readonly PropertyInfo[] UpdatedStateProperties
                = typeof(Provider).GetProperties();

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
                if (!AreEqual(_priorStateAccessor.PropertyType, _priorStateAccessor.GetValue(providerVariationContext.PriorState.Provider), _updatedStateAccessor.GetValue(providerVariationContext.UpdatedProvider)))
                {
                    providerVariationContext.AddVariationReasons(_variationReason);
                }
            }

            private bool AreEqual(Type typeOfValue, object? priorValue, object? updatedValue)
            {
                if (typeOfValue.IsEnum)
                {
                    return (int)priorValue == (int)updatedValue;
                }

                if (typeOfValue == typeof(DateTimeOffset) || typeOfValue == typeof(DateTimeOffset?))
                {
                    return (DateTimeOffset?)priorValue == (DateTimeOffset?)updatedValue;
                }

                return (string)priorValue == (string)updatedValue;
            }
        }

        private class VariationCheckWithSchemaVersions
        {
            public VariationCheckWithSchemaVersions(VariationCheck variationCheck, List<string> applicableSchemaVersions)
            {
                VariationCheck = variationCheck;
                ApplicableSchemaVersions = applicableSchemaVersions ?? new List<string>();
            }

            public VariationCheck VariationCheck { get; }
            public List<string> ApplicableSchemaVersions { get; }
        }

        static ProviderMetadataVariationStrategy()
        {
            VariationChecks = typeof(Provider)
                .GetProperties()
                .Where(_ => _.GetCustomAttributes(typeof(VariationReasonValueAttribute)).Any())
                .Select(_ => new VariationCheckWithSchemaVersions(new VariationCheck(_), 
                                                                ((VariationReasonValueAttribute)_.GetCustomAttributes(typeof(VariationReasonValueAttribute)).First()).ApplicableSchemaVersions?.ToList()))
                .ToArray();
        }

        private static readonly VariationCheckWithSchemaVersions[] VariationChecks;

        public string Name => "ProviderMetadata";

        public async Task<VariationStrategyResult> DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            if (providerVariationContext.PriorState == null || providerVariationContext.UpdatedProvider == null || providerVariationContext.ReleasedState == null)
            {
                return StrategyResult;
            }

            string schemaVersion = await providerVariationContext.GetReleasedStateSchemaVersion();

            if (string.IsNullOrWhiteSpace(schemaVersion))
                return StrategyResult;

            IList<VariationCheck> variationChecksToPerform = VariationChecks.Where(x => !x.ApplicableSchemaVersions.Any()).Select(x => x.VariationCheck) // No applicable schema versions
                                                            .Concat(VariationChecks.Where(x => x.ApplicableSchemaVersions.Contains(schemaVersion)).Select(x => x.VariationCheck)) // specific schema versions
                                                            .ToList();

            foreach (VariationCheck variationCheck in variationChecksToPerform)
            {
                variationCheck.Run(providerVariationContext);
            }

            if (providerVariationContext.VariationReasons.AnyWithNullCheck())
            {
                providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext));
            }

            return StrategyResult;
        }
    }
}
