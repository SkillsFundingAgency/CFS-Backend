using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Errors
{
    public abstract class PublishedProviderErrorDetector : IDetectPublishedProviderErrors
    {
        public async Task DetectErrors(PublishedProviderVersion publishedProviderVersion)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));

            ErrorCheck errorCheck = await HasErrors(publishedProviderVersion);

            if (errorCheck.HasErrors)
            {
                publishedProviderVersion.AddErrors(errorCheck.Errors);
            }
        }

        protected abstract Task<ErrorCheck> HasErrors(PublishedProviderVersion publishedProviderVersion);

        protected class ErrorCheck
        {
            public bool HasErrors => Errors?.Any() == true;
            
            public ICollection<PublishedProviderError> Errors {get; } = new List<PublishedProviderError>();

            public void AddError(PublishedProviderError error)
            {
                Errors.Add(error);
            }
        }
    }
}