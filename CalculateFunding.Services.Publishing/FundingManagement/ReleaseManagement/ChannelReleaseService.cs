using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ChannelReleaseService
    {
        private readonly IPublishedProvidersLoadContext _publishProvidersLoadContext;
        private readonly IProvidersForChannelFilterService _providersForChannelFilterService;
        private readonly IChannelOrganisationGroupGeneratorService _channelOrganisationGroupGeneratorService;
        private readonly IChannelOrganisationGroupChangeDetector _channelOrganisationGroupChangeDetector;
        private readonly IReleaseProviderPersistanceService _releaseProviderPersistanceService;
        private readonly IProviderVersionReleaseService _providerVersionReleaseService;

        public ChannelReleaseService(
            IPublishedProvidersLoadContext publishProvidersLoadContext,
            IProvidersForChannelFilterService providersForChannelFilterService,
            IChannelOrganisationGroupGeneratorService channelOrganisationGroupGeneratorService,
            IChannelOrganisationGroupChangeDetector channelOrganisationGroupChangeDetector,
            IReleaseProviderPersistanceService releaseProviderPersistanceService,
            IProviderVersionReleaseService providerVersionReleaseService)
        {
            Guard.ArgumentNotNull(publishProvidersLoadContext, nameof(publishProvidersLoadContext));
            Guard.ArgumentNotNull(providersForChannelFilterService, nameof(providersForChannelFilterService));
            Guard.ArgumentNotNull(channelOrganisationGroupGeneratorService, nameof(channelOrganisationGroupGeneratorService));
            Guard.ArgumentNotNull(channelOrganisationGroupChangeDetector, nameof(channelOrganisationGroupChangeDetector));
            Guard.ArgumentNotNull(releaseProviderPersistanceService, nameof(releaseProviderPersistanceService));
            Guard.ArgumentNotNull(providerVersionReleaseService, nameof(providerVersionReleaseService));

            _publishProvidersLoadContext = publishProvidersLoadContext;
            _providersForChannelFilterService = providersForChannelFilterService;
            _channelOrganisationGroupGeneratorService = channelOrganisationGroupGeneratorService;
            _channelOrganisationGroupChangeDetector = channelOrganisationGroupChangeDetector;
            _releaseProviderPersistanceService = releaseProviderPersistanceService;
            _providerVersionReleaseService = providerVersionReleaseService;
        }

        public async Task ReleaseProvidersForChannel(Channel channel, 
            FundingConfiguration fundingConfiguration, 
            SpecificationSummary specification, 
            IEnumerable<string> batchProviderIds)
        {
            Guard.ArgumentNotNull(channel, nameof(channel));

            IEnumerable<PublishedProvider> publishedProvidersBatch = await _publishProvidersLoadContext.GetOrLoadProviders(batchProviderIds);

            Dictionary<string, PublishedProviderVersion> providersToRelease = _providersForChannelFilterService.FilterProvidersForChannel(channel,
                publishedProvidersBatch.Select(_ => _.Released),
                fundingConfiguration)
                .ToDictionary(_ => _.ProviderId);

            IEnumerable<OrganisationGroupResult> allOrganisationGroupsForBatch = 
                await _channelOrganisationGroupGeneratorService.GenerateOrganisationGroups(channel, 
                    fundingConfiguration, 
                    specification, 
                    providersToRelease.Values);

            IEnumerable<OrganisationGroupResult> fundingGroupsToCreateForBatch = 
                await _channelOrganisationGroupChangeDetector.DetermineFundingGroupsToCreateBasedOnProviderVersions(allOrganisationGroupsForBatch, 
                    specification, 
                    channel);

            IEnumerable<PublishedProviderVersion> providersInGroupsToRelease = fundingGroupsToCreateForBatch
                .SelectMany(_ => _.Providers)
                .Select(_ => _.ProviderId)
                .Distinct()
                .Select(_ => providersToRelease[_]);

            await _releaseProviderPersistanceService.ReleaseProviders(
                providersInGroupsToRelease.Select(_ => _.ProviderId),
                specification.Id);

            await _providerVersionReleaseService.ReleaseProviderVersions(
                providersInGroupsToRelease,
                specification.Id);
        }
    }
}
