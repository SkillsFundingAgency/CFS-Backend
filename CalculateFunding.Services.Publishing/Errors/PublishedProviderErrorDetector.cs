using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
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

        public async Task<bool> DetectErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            Guard.ArgumentNotNull(publishedProvider, nameof(publishedProvider));

            bool updated = ClearErrors(publishedProvider.Current);

            ErrorCheck errorCheck = await HasErrors(publishedProvider, publishedProvidersContext);

            if (errorCheck.HasErrors)
            {
                updated = true;

                // reset the current to the version before the last refresh so nothing is overwritten until all errors are cleared
                if (publishedProvidersContext != null && 
                    publishedProvidersContext.VariationContexts.AnyWithNullCheck() && 
                    publishedProvidersContext.VariationContexts.ContainsKey(publishedProvider.Current.ProviderId))
                {
                    PublishedProviderVersion previousPublishedProvider = publishedProvidersContext.VariationContexts[publishedProvider.Current.ProviderId].CurrentState;
                    
                    // copy current state before any updates from current refresh
                    publishedProvider.Current.FundingLines = previousPublishedProvider.FundingLines;
                    publishedProvider.Current.Calculations = previousPublishedProvider.Calculations;
                    publishedProvider.Current.ReferenceData = previousPublishedProvider.ReferenceData;
                    publishedProvider.Current.TotalFunding = previousPublishedProvider.TotalFunding;
                    publishedProvider.Current.TemplateVersion = previousPublishedProvider.TemplateVersion;
                    publishedProvider.Current.Provider = previousPublishedProvider.Provider;
                }

                publishedProvider.Current.AddErrors(errorCheck.Errors);
            }

            return updated;
        }

        protected bool ClearErrors(PublishedProviderVersion publishedProviderVersion) => (publishedProviderVersion.Errors?.RemoveAll(_ => _.Type == ProviderErrorType)>0) ? true : false;

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