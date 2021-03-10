using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class PostPaymentOutOfScopeProviderErrorDetector : PublishedProviderErrorDetector
    {
        public PostPaymentOutOfScopeProviderErrorDetector() : base(PublishedProviderErrorType.PostPaymentOutOfScopeProvider)
        {
        }

        public override bool IsPreVariationCheck => true;

        public override bool IsAssignProfilePatternCheck => false;
        
        public override string Name => nameof(PostPaymentOutOfScopeProviderErrorDetector);

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            Guard.ArgumentNotNull(publishedProvidersContext, nameof(publishedProvidersContext));

            ErrorCheck errorCheck = new ErrorCheck();

            // if there is no released version then we don't need to do the check
            if (publishedProvider.Released == null)
            {
                return Task.FromResult(errorCheck);
            }

            if (!publishedProvidersContext.ScopedProviders.Select(_ => _.ProviderId).Contains(publishedProvider.Current.ProviderId))
            {
                errorCheck.AddError(new PublishedProviderError
                {
                    Type = PublishedProviderErrorType.PostPaymentOutOfScopeProvider,
                    DetailedErrorMessage = $"Provider {publishedProvider.Current.ProviderId} does not exists on in scope providers of specification {publishedProvidersContext.SpecificationId}",
                    SummaryErrorMessage = "Post Payment - Provider is not in scope of specification",
                    FundingStreamId = publishedProvider.Current.FundingStreamId,
                    Identifier = string.Empty,
                    FundingLineCode = string.Empty
                });
            }

            return Task.FromResult(errorCheck);
        }
    }
}
