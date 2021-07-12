using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Errors
{
    public abstract class OrganisationGroupsErrorDetectorBase : PublishedProviderErrorDetector
    {
        public override bool IsPreVariationCheck => true;

        public override bool IsAssignProfilePatternCheck => false;

        public OrganisationGroupsErrorDetectorBase(PublishedProviderErrorType errorType) : base(errorType)
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

            static string OrganisationGroupsKey(string fundingStreamId,
                string fundingPeriodId)
            {
                return $"{fundingStreamId}:{fundingPeriodId}";
            }

            string keyForOrganisationGroups = OrganisationGroupsKey(publishedProvider.Current.FundingStreamId, publishedProvider.Current.FundingPeriodId);

            if (publishedProvidersContext.OrganisationGroupResultsData.ContainsKey(keyForOrganisationGroups))
            {
                IEnumerable<OrganisationGroupResult> organisationGroups = publishedProvidersContext.OrganisationGroupResultsData[keyForOrganisationGroups];

                CheckGroups(publishedProvider,
                    organisationGroups.Where(_ => _.Providers.AnyWithNullCheck()
                        && _.Providers.Any(p => p.ProviderId == publishedProvider.Current.ProviderId)),
                    publishedProvidersContext,
                    errorCheck);
            }

            return Task.FromResult(errorCheck);
        }

        protected abstract void CheckGroups(PublishedProvider publishedProvider, 
            IEnumerable<OrganisationGroupResult> organisationGroups, 
            PublishedProvidersContext publishedProvidersContext, 
            ErrorCheck errorCheck);

        protected abstract bool SkipCheck(PublishedProvider publishedProvider);
    }
}
