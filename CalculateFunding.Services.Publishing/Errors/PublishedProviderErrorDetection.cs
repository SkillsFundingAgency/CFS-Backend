using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

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

        public async Task ProcessPublishedProvider(PublishedProviderVersion publishedProviderVersion)
        {
            publishedProviderVersion.ResetErrors();
            
            foreach (IDetectPublishedProviderErrors errorDetector in _errorDetectors)
            {
                await errorDetector.DetectErrors(publishedProviderVersion);
            }
        }
    }
}