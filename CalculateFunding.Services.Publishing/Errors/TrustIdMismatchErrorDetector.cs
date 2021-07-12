using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class TrustIdMismatchErrorDetector : OrganisationGroupsErrorDetectorBase
    {
        public override string Name => nameof(TrustIdMismatchErrorDetector);

        public TrustIdMismatchErrorDetector() : base(PublishedProviderErrorType.TrustIdMismatch)
        {
        }

        protected override void CheckGroups(PublishedProvider publishedProvider, 
            IEnumerable<OrganisationGroupResult> organisationGroups, 
            PublishedProvidersContext publishedProvidersContext,
            ErrorCheck errorCheck)
        {
            HashSet<string> organisationGroupsHashSet = organisationGroups.SelectMany(_ => _.Identifiers.Select(_ => $"{_.Type}-{_.Value}")).Distinct().ToHashSet();

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