using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public abstract class PublishedProviderErrorDetector : IDetectPublishedProviderErrors
    {
        public async Task DetectErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            Guard.ArgumentNotNull(publishedProvider, nameof(publishedProvider));

            ClearErrors(publishedProvider.Current);

            ErrorCheck errorCheck = await HasErrors(publishedProvider, publishedProvidersContext);

            if (errorCheck.HasErrors)
            {
                publishedProvider.Current.AddErrors(errorCheck.Errors);
            }
        }

        protected abstract void ClearErrors(PublishedProviderVersion publishedProviderVersion);

        protected abstract Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext);

        protected class ErrorCheck
        {
            public bool HasErrors => Errors?.Any() == true;
            
            public ICollection<PublishedProviderError> Errors { get; } = new List<PublishedProviderError>();

            public void AddError(PublishedProviderError error)
            {
                Errors.Add(error);
            }
        }
    }
}