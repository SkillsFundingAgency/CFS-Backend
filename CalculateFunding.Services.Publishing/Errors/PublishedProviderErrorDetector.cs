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
        protected readonly PublishedProviderErrorType ProviderErrorType;

        protected PublishedProviderErrorDetector(PublishedProviderErrorType providerErrorType)
        {
            ProviderErrorType = providerErrorType;
        }

        public virtual bool IsForAllFundingConfigurations => false;

        public abstract bool IsAssignProfilePatternCheck { get; }

        public virtual bool IsPostVariationCheck => false;

        public abstract string Name { get; }

        public abstract bool IsPreVariationCheck { get; }

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

        protected void ClearErrors(PublishedProviderVersion publishedProviderVersion)
        => publishedProviderVersion.Errors?.RemoveAll(_ => _.Type == ProviderErrorType);

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