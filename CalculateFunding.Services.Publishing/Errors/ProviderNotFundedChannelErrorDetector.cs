using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class ProviderNotFundedChannelErrorDetector : ChannelOrganisationGroupsErrorDetectorBase
    {
        public override string Name => nameof(ProviderNotFundedChannelErrorDetector);

        public ProviderNotFundedChannelErrorDetector() : base(PublishedProviderErrorType.ProviderNotFunded)
        {
        }

        protected override void CheckGroups(PublishedProvider publishedProvider, 
            PublishedProvidersContext publishedProvidersContext,
            ErrorCheck errorCheck)
        {
            IEnumerable<OrganisationGroupResult> organisationGroups = publishedProvidersContext.ChannelOrganisationGroupResultsData.SelectMany(_ => _.Value)
                        .Where(_ => _.Providers.AnyWithNullCheck()
                        && _.Providers.Any(p => p.ProviderId == publishedProvider.Current.ProviderId));

            if (organisationGroups.IsNullOrEmpty())
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

        protected override bool SkipCheck(PublishedProvider publishedProvider)
        {
            return false;
        }
    }
}