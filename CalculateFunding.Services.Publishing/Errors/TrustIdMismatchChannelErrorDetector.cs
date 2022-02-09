using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class TrustIdMismatchChannelErrorDetector : ChannelOrganisationGroupsErrorDetectorBase
    {
        public override string Name => nameof(TrustIdMismatchChannelErrorDetector);

        public TrustIdMismatchChannelErrorDetector() : base(PublishedProviderErrorType.TrustIdMismatch)
        {
        }

        protected override void CheckGroups(PublishedProvider publishedProvider, 
            PublishedProvidersContext publishedProvidersContext,
            ErrorCheck errorCheck)
        {
            HashSet<string> organisationGroupsHashSet = publishedProvidersContext.ChannelOrganisationGroupResultsData.SelectMany(_ => _.Value)
                .SelectMany(_ => _.Identifiers.Select(_ => $"{_.Type}-{_.Value}")).Distinct().ToHashSet();

            publishedProvidersContext.CurrentPublishedFunding
                .ForEach(x =>
                {
                    if (organisationGroupsHashSet.Contains($"{x.Current.OrganisationGroupTypeIdentifier}-{x.Current.OrganisationGroupIdentifierValue}")
                        && x.Current.ProviderFundings.All(pv => pv != publishedProvider.Released.FundingId))
                    {
                        errorCheck.AddError(new PublishedProviderError
                        {
                            Type = PublishedProviderErrorType.TrustIdMismatch,
                            DetailedErrorMessage = $"TrustId {x.Current.OrganisationGroupTypeIdentifier}-{x.Current.OrganisationGroupIdentifierValue} not matched.",
                            SummaryErrorMessage = "TrustId not matched",
                            FundingStreamId = publishedProvider.Current.FundingStreamId
                        });
                    }
                });
        }

        protected override bool SkipCheck(PublishedProvider publishedProvider)
        {
            // if there is no released version then we don't need to do the check
            return publishedProvider.Released == null;
        }
    }
}