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
        private readonly IEnumerable<IDetectPublishedProviderErrors> _errorDetectors;

        public PublishedProviderErrorDetection(IEnumerable<IDetectPublishedProviderErrors> errorDetectors)
        {
            Guard.ArgumentNotNull(errorDetectors, nameof(errorDetectors));
            
            _errorDetectors = errorDetectors;
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
            foreach (IDetectPublishedProviderErrors errorDetector in _errorDetectors.Where(predicate))
            {
                await errorDetector.DetectErrors(publishedProvider, context);
            }
        }
    }
}