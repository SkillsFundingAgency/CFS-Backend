using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ChannelOrganisationGroupChangeDetector : IChannelOrganisationGroupChangeDetector
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IPublishedProvidersLoadContext _publishedProvidersLoadContext;

        public ChannelOrganisationGroupChangeDetector(IReleaseManagementRepository releaseManagementRepository,
            IPublishedProvidersLoadContext publishedProvidersLoadContext)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(publishedProvidersLoadContext, nameof(publishedProvidersLoadContext));

            _repo = releaseManagementRepository;
            _publishedProvidersLoadContext = publishedProvidersLoadContext;
        }

        public async Task<IEnumerable<OrganisationGroupResult>> DetermineFundingGroupsToCreateBasedOnProviderVersions(IEnumerable<OrganisationGroupResult> channelOrganisationGroups, SpecificationSummary specification, Channel channel)
        {
            IEnumerable<LatestProviderVersionInFundingGroup> latestProviderVersionInFundingGroups = await _repo.GetLatestProviderVersionInFundingGroups(specification.Id, channel.ChannelId);

            Dictionary<string, IEnumerable<LatestProviderVersionInFundingGroup>> existingGroupProviders = GroupProviders(latestProviderVersionInFundingGroups);

            List<OrganisationGroupResult> organisationGroupsToCreateNewVersions = new List<OrganisationGroupResult>();

            foreach (OrganisationGroupResult organisationGroupResult in channelOrganisationGroups)
            {
                string groupingKey = GenerateGroupingKey(organisationGroupResult);

                if (!existingGroupProviders.TryGetValue(groupingKey, out IEnumerable<LatestProviderVersionInFundingGroup> existingProviders))
                {
                    organisationGroupsToCreateNewVersions.Add(organisationGroupResult);
                    continue;
                }

                if (GroupContainsDifferentProviderIds(organisationGroupResult, existingProviders))
                {
                    organisationGroupsToCreateNewVersions.Add(organisationGroupResult);
                    continue;
                }

                IEnumerable<PublishedProvider> providers = await _publishedProvidersLoadContext.GetOrLoadProviders(organisationGroupResult.Providers.Select(_ => _.ProviderId));

                foreach (Common.ApiClient.Providers.Models.Provider provider in organisationGroupResult.Providers)
                {
                    LatestProviderVersionInFundingGroup existingProvider = existingProviders.First(_ => _.ProviderId == provider.ProviderId);

                    PublishedProvider currentProvider = providers.First(_ => _.Current.ProviderId == provider.ProviderId);

                    if (currentProvider.Released.MajorVersion != existingProvider.MajorVersion)
                    {
                        organisationGroupsToCreateNewVersions.Add(organisationGroupResult);
                        break;
                    }
                }
            }

            return organisationGroupsToCreateNewVersions;
        }

        private static bool GroupContainsDifferentProviderIds(OrganisationGroupResult organisationGroupResult, IEnumerable<LatestProviderVersionInFundingGroup> existingProviders)
        {
            IEnumerable<string> existingProviderIds = existingProviders.Select(_ => _.ProviderId).OrderBy(_ => _);
            IEnumerable<string> groupProviderIds = organisationGroupResult.Providers.Select(_ => _.ProviderId).OrderBy(_ => _);

            return !existingProviderIds.SequenceEqual(groupProviderIds);
        }

        private Dictionary<string, IEnumerable<LatestProviderVersionInFundingGroup>> GroupProviders(IEnumerable<LatestProviderVersionInFundingGroup> latestProviderVersionInFundingGroups)
        {
            IEnumerable<IGrouping<string, LatestProviderVersionInFundingGroup>> groups = latestProviderVersionInFundingGroups.GroupBy(_ => GenerateGroupingKey(_));
            Dictionary<string, IEnumerable<LatestProviderVersionInFundingGroup>> result = new Dictionary<string, IEnumerable<LatestProviderVersionInFundingGroup>>(groups.Count());

            foreach (IGrouping<string, LatestProviderVersionInFundingGroup> group in groups)
            {
                result.Add(group.Key, group);
            }

            return result;
        }

        private string GenerateGroupingKey(OrganisationGroupResult group)
        {
            return $"{group.GroupReason}-{group.GroupTypeCode}-{group.IdentifierValue}";
        }

        private string GenerateGroupingKey(LatestProviderVersionInFundingGroup group)
        {
            return $"{group.GroupingReasonCode}-{group.OrganisationGroupTypeCode}-{group.OrganisationGroupIdentifierValue}";
        }
    }
}
