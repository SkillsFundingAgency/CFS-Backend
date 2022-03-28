using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class ProviderNotFundedErrorDetector : OrganisationGroupsErrorDetectorBase
    {
        public override string Name => nameof(ProviderNotFundedErrorDetector);

        public ProviderNotFundedErrorDetector() : base(PublishedProviderErrorType.ProviderNotFunded)
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
                IEnumerable<OrganisationGroupResult> organisationGroupsHashSet = Enumerable.DistinctBy(organisationGroups, _ => _.Identifiers.Select(_ => $"{_.Type}-{_.Value}"));

                if (!organisationGroups.AnyWithNullCheck(_ => _.GroupReason == OrganisationGroupingReason.Payment || _.GroupReason == OrganisationGroupingReason.Contracting))
                {
                    string errorMessage = $"Provider {publishedProvider.Current.ProviderId} not configured to be a member of any group.";
                    errorCheck.AddError(new PublishedProviderError
                    {
                        Type = PublishedProviderErrorType.ProviderNotFunded,
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