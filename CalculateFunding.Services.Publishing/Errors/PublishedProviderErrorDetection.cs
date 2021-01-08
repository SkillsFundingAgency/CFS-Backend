using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class PublishedProviderErrorDetection : IPublishedProviderErrorDetection
    {
        private readonly IErrorDetectionStrategyLocator _errorDetectorLocator;

        public PublishedProviderErrorDetection(IErrorDetectionStrategyLocator errorDetectorLocator)
        {
            Guard.ArgumentNotNull(errorDetectorLocator, nameof(errorDetectorLocator));

            _errorDetectorLocator = errorDetectorLocator;
        }

        public async Task ProcessPublishedProvider(PublishedProvider publishedProvider,
            PublishedProvidersContext context)
        {
            await ProcessPublishedProvider(publishedProvider, _ => true, context);
        }

        public async Task ProcessPublishedProvider(PublishedProvider publishedProvider,
            Func<IDetectPublishedProviderErrors, bool> predicate,
            PublishedProvidersContext context)
        {
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors = context.FundingConfiguration?.ErrorDetectors?.Select(_ => _errorDetectorLocator.GetDetector(_));

            if (errorDetectors.AnyWithNullCheck())
            {
                foreach (IDetectPublishedProviderErrors errorDetector in errorDetectors.Where(predicate))
                {
                    await errorDetector.DetectErrors(publishedProvider, context);
                }
            }
        }
    }
}