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

        public async Task<bool> ApplyRefreshPreVariationErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context)
        {
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors = GetErrorDetectorsForFundingConfiguration(context)?
                .Where(_ => _.IsPreVariationCheck);

            return await ProcessPublishedProviderWithErrorDetectors(publishedProvider, errorDetectors, context);
        }

        public async Task<bool> ApplyRefreshPostVariationsErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context)
        {
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors = GetErrorDetectorsForFundingConfiguration(context)?
                .Where(_ => _.IsPostVariationCheck);

            return await ProcessPublishedProviderWithErrorDetectors(publishedProvider, errorDetectors, context);
        }

        public async Task<bool> ApplyAssignProfilePatternErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context)
        {
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors = GetErrorDetectorsForFundingConfiguration(context)?
                .Where(_ => _.IsAssignProfilePatternCheck);

            return await ProcessPublishedProviderWithErrorDetectors(publishedProvider, errorDetectors, context);
        }

        private IEnumerable<IDetectPublishedProviderErrors> GetErrorDetectorsForFundingConfiguration(PublishedProvidersContext context)
        {
            return _errorDetectorLocator.GetErrorDetectorsForAllFundingConfigurations()
                .Concat(context.FundingConfiguration?.ErrorDetectors?.Select(_ => _errorDetectorLocator.GetErrorDetectorByName(_)) ?? ArraySegment<IDetectPublishedProviderErrors>.Empty)
                .OrderByDescending(_ => _.RunningOrder);
        }

        private async Task<bool> ProcessPublishedProviderWithErrorDetectors(PublishedProvider publishedProvider,
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors,
            PublishedProvidersContext context)
        {
            bool updated = false;

            foreach (IDetectPublishedProviderErrors errorDetector in errorDetectors ?? ArraySegment<IDetectPublishedProviderErrors>.Empty)
            {
                if (await errorDetector.DetectErrors(publishedProvider, context))
                {
                    updated = true;
                }
            }

            return updated;
        }
    }
}