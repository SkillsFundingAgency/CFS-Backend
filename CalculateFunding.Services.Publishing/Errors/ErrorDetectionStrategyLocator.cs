using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class ErrorDetectionStrategyLocator : IErrorDetectionStrategyLocator
    {
        private readonly IDictionary<string, IDetectPublishedProviderErrors> _detectorStrategies;

        public ErrorDetectionStrategyLocator(IEnumerable<IDetectPublishedProviderErrors> registeredStrategies)
        {
            _detectorStrategies = new ConcurrentDictionary<string, IDetectPublishedProviderErrors>(
                registeredStrategies.ToDictionary(_ => _.Name));
        }

        public IDetectPublishedProviderErrors GetDetector(string errorDetectorName)
        {
            Guard.IsNullOrWhiteSpace(errorDetectorName, nameof(errorDetectorName));

            if (_detectorStrategies.TryGetValue(errorDetectorName, out IDetectPublishedProviderErrors detectorStrategy))
            {
                return detectorStrategy;
            }

            throw new ArgumentOutOfRangeException(nameof(errorDetectorName), 
                $"Unable to find a registered error detector strategy with name: {errorDetectorName}");
        }
    }
}
