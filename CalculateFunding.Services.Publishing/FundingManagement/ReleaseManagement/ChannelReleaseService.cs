using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ChannelReleaseService : IChannelReleaseService
    {
        private readonly IPublishedProvidersLoadContext _publishProvidersLoadContext;
        private readonly IProvidersForChannelFilterService _providersForChannelFilterService;
        private readonly IChannelOrganisationGroupGeneratorService _channelOrganisationGroupGeneratorService;
        private readonly IChannelOrganisationGroupChangeDetector _channelOrganisationGroupChangeDetector;
        private readonly IReleaseProviderPersistenceService _releaseProviderPersistenceService;
        private readonly IProviderVersionReleaseService _providerVersionReleaseService;
        private readonly IProviderVersionToChannelReleaseService _providerVersionToChannelReleaseService;
        private readonly IGenerateVariationReasonsForChannelService _generateVariationReasonsForChannelService;
        private readonly IProviderVariationReasonsReleaseService _providerVariationReasonsReleaseService;
        private readonly IPublishedProviderContentChannelPersistenceService _publishedProviderContentChannelPersistenceService;
        private readonly IPublishedFundingContentsChannelPersistenceService _publishedFundingContentsChannelPersistenceService;
        private readonly IFundingGroupProviderPersistenceService _fundingGroupProviderPersistenceService;
        private readonly IFundingGroupService _fundingGroupService;
        private readonly IFundingGroupDataGenerator _fundingGroupDataGenerator;
        private readonly IFundingGroupDataPersistenceService _fundingGroupDataPersistenceService;
        private readonly ICurrentDateTime _currentDateTime;

        public ChannelReleaseService(
            IPublishedProvidersLoadContext publishProvidersLoadContext,
            IProvidersForChannelFilterService providersForChannelFilterService,
            IChannelOrganisationGroupGeneratorService channelOrganisationGroupGeneratorService,
            IChannelOrganisationGroupChangeDetector channelOrganisationGroupChangeDetector,
            IReleaseProviderPersistenceService releaseProviderPersistenceService,
            IProviderVersionReleaseService providerVersionReleaseService,
            IProviderVersionToChannelReleaseService providerVersionToChannelReleaseService,
            IGenerateVariationReasonsForChannelService generateVariationReasonsForChannelService,
            IProviderVariationReasonsReleaseService providerVariationReasonsReleaseService,
            IPublishedProviderContentChannelPersistenceService publishedProviderContentChannelPersistenceService,
            IPublishedFundingContentsChannelPersistenceService publishedFundingContentsChannelPersistenceService,
            IFundingGroupProviderPersistenceService fundingGroupProviderPersistenceService,
            IFundingGroupService fundingGroupService,
            IFundingGroupDataGenerator fundingGroupDataGenerator,
            IFundingGroupDataPersistenceService fundingGroupDataPersistenceService,
            ICurrentDateTime currentDateTime)
        {
            Guard.ArgumentNotNull(publishProvidersLoadContext, nameof(publishProvidersLoadContext));
            Guard.ArgumentNotNull(providersForChannelFilterService, nameof(providersForChannelFilterService));
            Guard.ArgumentNotNull(channelOrganisationGroupGeneratorService, nameof(channelOrganisationGroupGeneratorService));
            Guard.ArgumentNotNull(channelOrganisationGroupChangeDetector, nameof(channelOrganisationGroupChangeDetector));
            Guard.ArgumentNotNull(releaseProviderPersistenceService, nameof(releaseProviderPersistenceService));
            Guard.ArgumentNotNull(providerVersionReleaseService, nameof(providerVersionReleaseService));
            Guard.ArgumentNotNull(providerVersionToChannelReleaseService, nameof(providerVersionToChannelReleaseService));
            Guard.ArgumentNotNull(generateVariationReasonsForChannelService, nameof(generateVariationReasonsForChannelService));
            Guard.ArgumentNotNull(providerVariationReasonsReleaseService, nameof(providerVariationReasonsReleaseService));
            Guard.ArgumentNotNull(publishedProviderContentChannelPersistenceService, nameof(publishedProviderContentChannelPersistenceService));
            Guard.ArgumentNotNull(publishedFundingContentsChannelPersistenceService, nameof(publishedFundingContentsChannelPersistenceService));
            Guard.ArgumentNotNull(fundingGroupService, nameof(fundingGroupService));
            Guard.ArgumentNotNull(fundingGroupProviderPersistenceService, nameof(fundingGroupProviderPersistenceService));
            Guard.ArgumentNotNull(fundingGroupDataGenerator, nameof(fundingGroupDataGenerator));
            Guard.ArgumentNotNull(fundingGroupDataPersistenceService, nameof(fundingGroupDataPersistenceService));
            Guard.ArgumentNotNull(currentDateTime, nameof(currentDateTime));

            _publishProvidersLoadContext = publishProvidersLoadContext;
            _providersForChannelFilterService = providersForChannelFilterService;
            _channelOrganisationGroupGeneratorService = channelOrganisationGroupGeneratorService;
            _channelOrganisationGroupChangeDetector = channelOrganisationGroupChangeDetector;
            _releaseProviderPersistenceService = releaseProviderPersistenceService;
            _providerVersionReleaseService = providerVersionReleaseService;
            _providerVersionToChannelReleaseService = providerVersionToChannelReleaseService;
            _generateVariationReasonsForChannelService = generateVariationReasonsForChannelService;
            _providerVariationReasonsReleaseService = providerVariationReasonsReleaseService;
            _publishedProviderContentChannelPersistenceService = publishedProviderContentChannelPersistenceService;
            _publishedFundingContentsChannelPersistenceService = publishedFundingContentsChannelPersistenceService;
            _fundingGroupProviderPersistenceService = fundingGroupProviderPersistenceService;
            _fundingGroupService = fundingGroupService;
            _fundingGroupDataGenerator = fundingGroupDataGenerator;
            _fundingGroupDataPersistenceService = fundingGroupDataPersistenceService;
            _currentDateTime = currentDateTime;
        }

        /// <summary>
        /// Release providers for channel. Note all database writes need to use ambient transaction
        /// </summary>
        /// <param name="channel">The channel to release to</param>
        /// <param name="fundingConfiguration">The funding configuration</param>
        /// <param name="specification">The specification</param>
        /// <param name="batchPublishedProviderIds">The batch of published provider ids to release</param>
        /// <returns></returns>
        public async Task ReleaseProvidersForChannel(Channel channel,
            FundingConfiguration fundingConfiguration,
            SpecificationSummary specification,
            IEnumerable<string> batchPublishedProviderIds,
            Reference author,
            string jobId,
            string correlationId)
        {
            Guard.ArgumentNotNull(channel, nameof(channel));
            Guard.ArgumentNotNull(fundingConfiguration, nameof(fundingConfiguration));
            Guard.ArgumentNotNull(specification, nameof(specification));
            Guard.ArgumentNotNull(batchPublishedProviderIds, nameof(batchPublishedProviderIds));

            DateTime currentDateTime = _currentDateTime.GetUtcNow();

            IEnumerable<PublishedProvider> publishedProvidersBatch = await _publishProvidersLoadContext.GetOrLoadProviders(batchPublishedProviderIds);

            Dictionary<string, PublishedProviderVersion> providersToRelease = _providersForChannelFilterService.FilterProvidersForChannel(channel,
                publishedProvidersBatch.Select(_ => _.Released),
                fundingConfiguration)
                .ToDictionary(_ => _.ProviderId);

            IEnumerable<OrganisationGroupResult> allOrganisationGroups =
                await _channelOrganisationGroupGeneratorService.GenerateOrganisationGroups(channel,
                    fundingConfiguration,
                    specification,
                    providersToRelease.Values);

            IEnumerable<OrganisationGroupResult> organisationGroupsToCreate =
                await _channelOrganisationGroupChangeDetector.DetermineFundingGroupsToCreateBasedOnProviderVersions(allOrganisationGroups,
                    specification,
                    channel);

            IEnumerable<FundingGroup> fundingGroups =
                await _fundingGroupService.CreateFundingGroups(specification.Id, channel.ChannelId, organisationGroupsToCreate);

            IEnumerable<GeneratedPublishedFunding> fundingGroupData =
                await _fundingGroupDataGenerator.Generate(organisationGroupsToCreate,
                                                          specification,
                                                          channel,
                                                          batchPublishedProviderIds,
                                                          author,
                                                          jobId,
                                                          correlationId);

            IEnumerable<FundingGroupVersion> fundingGroupVersionsCreated = await _fundingGroupDataPersistenceService.ReleaseFundingGroupData(fundingGroupData, channel.ChannelId);

            IEnumerable<PublishedProviderVersion> providersInGroupsToRelease = organisationGroupsToCreate
                .SelectMany(_ => _.Providers)
                .Select(_ => _.ProviderId)
                .Distinct()
                .Select(_ => providersToRelease[_]);

            IEnumerable<string> providersInGroupsToReleasedProviderIds = providersInGroupsToRelease.Select(_ => _.ProviderId);

            IEnumerable<ReleasedProvider> releasedProviders = await _releaseProviderPersistenceService.ReleaseProviders(
                providersInGroupsToReleasedProviderIds,
                specification.Id);

            await _providerVersionReleaseService.ReleaseProviderVersions(
                providersInGroupsToRelease,
                specification.Id);

            await _providerVersionToChannelReleaseService.ReleaseProviderVersionChannel(
                providersInGroupsToRelease.Select(_=>_.ProviderId),
                channel.ChannelId,
                currentDateTime);

            IDictionary<string, IEnumerable<VariationReason>> variationReasonsForProviders = await _generateVariationReasonsForChannelService.GenerateVariationReasonsForProviders(
                providersInGroupsToReleasedProviderIds,
                channel,
                specification,
                fundingConfiguration,
                allOrganisationGroups.GroupByProviderId());

            await _providerVariationReasonsReleaseService.PopulateReleasedProviderChannelVariationReasons(variationReasonsForProviders, channel);

            await _fundingGroupProviderPersistenceService.PersistFundingGroupProviders(channel.ChannelId, fundingGroupData, providersInGroupsToRelease);

            await _publishedProviderContentChannelPersistenceService
                .SavePublishedProviderContents(specification, providersInGroupsToRelease, channel, variationReasonsForProviders);

            await _publishedFundingContentsChannelPersistenceService
                .SavePublishedFundingContents(fundingGroupData.Select(_ => _.PublishedFundingVersion), channel);
        }
    }
}
