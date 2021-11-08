using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class NoApplicableVariationErrorDetector : PublishedProviderErrorDetector
    {
        public NoApplicableVariationErrorDetector()
            : base(PublishedProviderErrorType.NoApplicableVariation)
        {
        }

        public override bool IsPreVariationCheck => false;

        public override bool IsAssignProfilePatternCheck => false;

        public override bool IsPostVariationCheck => true;

        public override bool IsForAllFundingConfigurations => true;

        public override string Name => nameof(NoApplicableVariationErrorDetector);

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            ErrorCheck errorCheck = new ErrorCheck();

            // only add no applicable variation if the provider doesn't have custom profiles because if it has
            // custom profiles then it will always be updated
            if (publishedProvidersContext.VariationContexts.ContainsKey(publishedProvider.Current.ProviderId) && 
                publishedProvidersContext.VariationContexts[publishedProvider.Current.ProviderId].ApplicableVariations.Count == 0 &&
                !publishedProvider.Current.HasCustomProfiles)
            {
                string errorMessage = $"Provider {publishedProvider.Current.ProviderId} no applicable variation found for variances {publishedProvidersContext.VariationContexts[publishedProvider.Current.ProviderId].Variances.AsJson()}.";
                errorCheck.AddError(new PublishedProviderError
                {
                    Type = PublishedProviderErrorType.NoApplicableVariation,
                    DetailedErrorMessage = errorMessage,
                    SummaryErrorMessage = errorMessage,
                    FundingStreamId = publishedProvider.Current.FundingStreamId
                });
            }

            return Task.FromResult(errorCheck);
        }
    }
}
