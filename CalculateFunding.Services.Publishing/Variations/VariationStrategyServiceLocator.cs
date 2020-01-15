using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Variations
{
    public class VariationStrategyServiceLocator : IVariationStrategyServiceLocator
    {
        private readonly ConcurrentDictionary<string, IVariationStrategy> _variationStrategies;

        public VariationStrategyServiceLocator(IEnumerable<IVariationStrategy> registeredStrategies)
        {
            _variationStrategies = new ConcurrentDictionary<string, IVariationStrategy>(
                registeredStrategies.ToDictionary(_ => _.Name));
        }

        public IVariationStrategy GetService(string variationStrategyName)
        {
            Guard.IsNullOrWhiteSpace(variationStrategyName, nameof(variationStrategyName));

            if (_variationStrategies.TryGetValue(variationStrategyName, out  IVariationStrategy variationStrategy))
            {
                return variationStrategy;
            }

            throw new ArgumentOutOfRangeException(nameof(variationStrategyName), 
                $"Unable to find a registered variation strategy with name: {variationStrategyName}");
        }
    }
}
