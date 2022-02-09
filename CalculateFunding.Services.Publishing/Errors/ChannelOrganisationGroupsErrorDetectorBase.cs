using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Errors
{
    public abstract class ChannelOrganisationGroupsErrorDetectorBase : PublishedProviderErrorDetector
    {
        public override bool IsPreVariationCheck => true;

        public override bool IsAssignProfilePatternCheck => false;

        public ChannelOrganisationGroupsErrorDetectorBase(PublishedProviderErrorType errorType) : base(errorType)
        { 
        }

        protected override Task<ErrorCheck> HasErrors(
            PublishedProvider publishedProvider,
            PublishedProvidersContext publishedProvidersContext)
        {
            Guard.ArgumentNotNull(publishedProvidersContext, nameof(publishedProvidersContext));

            ErrorCheck errorCheck = new ErrorCheck();

            if (SkipCheck(publishedProvider))
            {
                return Task.FromResult(errorCheck);
            }

            if (publishedProvidersContext.ChannelOrganisationGroupResultsData.Any())
            {
                CheckGroups(publishedProvider,
                    publishedProvidersContext,
                    errorCheck);
            }

            return Task.FromResult(errorCheck);
        }

        protected abstract void CheckGroups(PublishedProvider publishedProvider, 
            PublishedProvidersContext publishedProvidersContext, 
            ErrorCheck errorCheck);

        protected abstract bool SkipCheck(PublishedProvider publishedProvider);
    }
}
