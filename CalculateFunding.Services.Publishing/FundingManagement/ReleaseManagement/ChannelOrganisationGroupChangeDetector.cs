using AutoMapper;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiClientProviderModels = CalculateFunding.Common.ApiClient.Providers.Models;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ChannelOrganisationGroupChangeDetector : IChannelOrganisationGroupChangeDetector
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IPublishedProvidersLoadContext _publishedProvidersLoadContext;
        private readonly IMapper _mapper;

        public ChannelOrganisationGroupChangeDetector(IReleaseManagementRepository releaseManagementRepository,
            IPublishedProvidersLoadContext publishedProvidersLoadContext,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(publishedProvidersLoadContext, nameof(publishedProvidersLoadContext));

            _repo = releaseManagementRepository;
            _publishedProvidersLoadContext = publishedProvidersLoadContext;
            _mapper = mapper;
        }

        public async Task<(IEnumerable<OrganisationGroupResult>, Dictionary<string, PublishedProviderVersion>)> DetermineFundingGroupsToCreateBasedOnProviderVersions(
            IEnumerable<OrganisationGroupResult> channelOrganisationGroups,
            Dictionary<string, PublishedProviderVersion> providersToRelease,
            SpecificationSummary specification, 
            Channel channel)
        {
            IEnumerable<LatestProviderVersionInFundingGroup> latestProviderVersionInFundingGroups 
                = await _repo.GetLatestProviderVersionInFundingGroups(specification.Id, channel.ChannelId);

            ILookup<string, LatestProviderVersionInFundingGroup> existingGroupProviders 
                = latestProviderVersionInFundingGroups.ToLookup(_ => GenerateGroupingKey(_));

            Dictionary<string, PublishedProviderVersion> providerVersionsInGroups = new Dictionary<string, PublishedProviderVersion>();

            List<OrganisationGroupResult> organisationGroupsToCreateNewVersions = new List<OrganisationGroupResult>();
            foreach (OrganisationGroupResult organisationGroupResult in channelOrganisationGroups)
            {
                string groupingKey = GenerateGroupingKey(organisationGroupResult);

                IEnumerable<LatestProviderVersionInFundingGroup> existingProviders = existingGroupProviders[groupingKey];
                if (!existingProviders.Any())
                {
                    organisationGroupsToCreateNewVersions.Add(organisationGroupResult);
                    continue;
                }

                IEnumerable<(string,int,int)> missingProviders = existingProviders.Where(p => organisationGroupResult.Providers.Select(_ => _.ProviderId).All(p2 => p2 != p.ProviderId)).Select(_ => (_.ProviderId, _.MajorVersion, _.LatestReleasedProviderMajorVersion));
                missingProviders = missingProviders.Where(_ => !providersToRelease.ContainsKey(_.Item1) && (_.Item2 == _.Item3));
                if (missingProviders.Any())
                {
                    
                    List<ApiClientProviderModels.Provider> organisationGroupProviders = organisationGroupResult.Providers.ToList();

                    foreach ((string, int, int) missingProvider in missingProviders)
                    {
                        PublishedProviderVersion providerVersion =
                            await _publishedProvidersLoadContext.LoadProviderVersion(missingProvider.Item1, missingProvider.Item2);

                        ApiClientProviderModels.Provider apiProvider = _mapper.Map<ApiClientProviderModels.Provider>(providerVersion?.Provider);

                        if (apiProvider != null)
                        {
                            providerVersionsInGroups.TryAdd(providerVersion.ProviderId, providerVersion);
                            organisationGroupProviders.Add(apiProvider);
                        }
                    }

                    organisationGroupResult.Providers = organisationGroupProviders;
                }

                if (GroupContainsDifferentProviderIds(organisationGroupResult, existingProviders))
                {
                    organisationGroupsToCreateNewVersions.Add(organisationGroupResult);
                    continue;
                }

                IEnumerable<PublishedProvider> providers = 
                    await _publishedProvidersLoadContext.GetOrLoadProviders(organisationGroupResult.Providers.Select(_ => _.ProviderId));

                foreach (ApiClientProviderModels.Provider provider in organisationGroupResult.Providers)
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

            IEnumerable<PublishedProvider> publishedProvidersInGroups = await _publishedProvidersLoadContext.GetOrLoadProviders(organisationGroupsToCreateNewVersions
                    .SelectMany(_ => _.Providers)
                    .Select(_ => _.ProviderId)
                    .Distinct());

            foreach (PublishedProviderVersion batchPublishedProviderVersion in publishedProvidersInGroups.Select(_ => _.Released))
            {
                providerVersionsInGroups.TryAdd(batchPublishedProviderVersion.ProviderId, batchPublishedProviderVersion);
            }

            return (organisationGroupsToCreateNewVersions, providerVersionsInGroups);
        }

        private static bool GroupContainsDifferentProviderIds(
            OrganisationGroupResult organisationGroupResult, 
            IEnumerable<LatestProviderVersionInFundingGroup> existingProviders)
        {
            IEnumerable<string> existingProviderIds = existingProviders.Select(_ => _.ProviderId).OrderBy(_ => _);
            IEnumerable<string> groupProviderIds = organisationGroupResult.Providers.Select(_ => _.ProviderId).OrderBy(_ => _);

            return !existingProviderIds.SequenceEqual(groupProviderIds);
        }

        private string GenerateGroupingKey(OrganisationGroupResult group)
            => $"{group.GroupReason}-{group.GroupTypeCode}-{group.IdentifierValue}";

        private string GenerateGroupingKey(LatestProviderVersionInFundingGroup group)
            => $"{group.GroupingReasonCode}-{group.OrganisationGroupTypeCode}-{group.OrganisationGroupIdentifierValue}";
    }
}
