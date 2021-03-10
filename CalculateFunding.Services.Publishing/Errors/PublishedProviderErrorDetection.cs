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

        public async Task ApplyRefreshPreVariationErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context)
        {
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors = GetErrorDetectorsForFundingConfiguration(context)?
                .Where(_ => _.IsPreVariationCheck);

            await ProcessPublishedProviderWithErrorDetectors(publishedProvider, errorDetectors, context);
        }

        public async Task ApplyRefreshPostVariationsErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context)
        {
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors = GetErrorDetectorsForFundingConfiguration(context)?
                .Where(_ => _.IsPostVariationCheck);

            await ProcessPublishedProviderWithErrorDetectors(publishedProvider, errorDetectors, context);
        }

        public async Task ApplyAssignProfilePatternErrorDetection(PublishedProvider publishedProvider,
            PublishedProvidersContext context)
        {
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors = GetErrorDetectorsForFundingConfiguration(context)?
                .Where(_ => _.IsAssignProfilePatternCheck);

            await ProcessPublishedProviderWithErrorDetectors(publishedProvider, errorDetectors, context);
        }

        private IEnumerable<IDetectPublishedProviderErrors> GetErrorDetectorsForFundingConfiguration(PublishedProvidersContext context)
        {
            return context.FundingConfiguration?.ErrorDetectors?.Select(_ => _errorDetectorLocator.GetErrorDetectorByName(_))
                .Concat(_errorDetectorLocator.GetErrorDetectorsForAllFundingConfigurations());
        }

        private async Task ProcessPublishedProviderWithErrorDetectors(PublishedProvider publishedProvider,
            IEnumerable<IDetectPublishedProviderErrors> errorDetectors,
            PublishedProvidersContext context)
        {
            foreach (IDetectPublishedProviderErrors errorDetector in errorDetectors ?? ArraySegment<IDetectPublishedProviderErrors>.Empty)
            {
                await errorDetector.DetectErrors(publishedProvider, context);
            }   
        }
    }
}