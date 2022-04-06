using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class ProviderNotMemberOfOrganisationGroupErrorDetector : OrganisationGroupsErrorDetectorBase
    {
        public override string Name => nameof(ProviderNotMemberOfOrganisationGroupErrorDetector);

        public ProviderNotMemberOfOrganisationGroupErrorDetector() : base(PublishedProviderErrorType.ProviderNotMemberOfOrganisationGroup)
        {
        }

        protected override void CheckGroups(PublishedProvider publishedProvider, 
            IEnumerable<OrganisationGroupResult> organisationGroups, 
            PublishedProvidersContext publishedProvidersContext,
            ErrorCheck errorCheck)
        {
            // bypass error check if the provider is indicative
            if (!publishedProvider.Current.IsIndicative)
            {
                if (organisationGroups.IsNullOrEmpty())
                {
                    string errorMessage = $"Provider {publishedProvider.Current.ProviderId} not configured to be a member of any organisation group.";
                    errorCheck.AddError(new PublishedProviderError
                    {
                        Type = PublishedProviderErrorType.ProviderNotMemberOfOrganisationGroup,
                        DetailedErrorMessage = errorMessage,
                        SummaryErrorMessage = errorMessage,
                        FundingStreamId = publishedProvider.Current.FundingStreamId
                    });
                }
            }
        }

        protected override bool SkipCheck(PublishedProvider publishedProvider)
        {
            return false;
        }
    }
}