using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Generators.OrganisationGroup.Models;
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
            HashSet<string> organisationGroupsHashSet = organisationGroups.SelectMany(_ => _.Identifiers.Select(_ => $"{_.Type}-{_.Value}")).Distinct().ToHashSet();

            if (organisationGroups.IsNullOrEmpty())
            {
                errorCheck.AddError(new PublishedProviderError
                {
                    Type = PublishedProviderErrorType.ProviderNotFunded,
                    DetailedErrorMessage = $"Provider {publishedProvider.Current.ProviderId} not configured to be a member of any group.",
                    SummaryErrorMessage = "Provider not funded",
                    FundingStreamId = publishedProvider.Current.FundingStreamId
                });
            }
        }

        protected override bool SkipCheck(PublishedProvider publishedProvider)
        {
            return false;
        }
    }
}