﻿using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Models;
using Serilog;
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
        private readonly IExistingReleasedProvidersForChannelFilterService _existingReleasedProvidersForChannelFilterService;
        private readonly IProviderVersionToChannelReleaseService _providerVersionToChannelReleaseService;
        private readonly IGenerateVariationReasonsForChannelService _generateVariationReasonsForChannelService;
        private readonly IProviderVariationReasonsReleaseService _providerVariationReasonsReleaseService;
        private readonly IPublishedProviderContentChannelPersistenceService _publishedProviderContentChannelPersistenceService;
        private readonly IPublishedFundingContentsChannelPersistenceService _publishedFundingContentsChannelPersistenceService;
        private readonly IFundingGroupProviderPersistenceService _fundingGroupProviderPersistenceService;
        private readonly IFundingGroupService _fundingGroupService;
        private readonly IFundingGroupDataGenerator _fundingGroupDataGenerator;
        private readonly IFundingGroupDataPersistenceService _fundingGroupDataPersistenceService;
        private readonly IReleaseManagementRepository _repo;
        private readonly ICurrentDateTime _currentDateTime;
        private readonly ILogger _logger;

        public ChannelReleaseService(
            IPublishedProvidersLoadContext publishProvidersLoadContext,
            IProvidersForChannelFilterService providersForChannelFilterService,
            IChannelOrganisationGroupGeneratorService channelOrganisationGroupGeneratorService,
            IChannelOrganisationGroupChangeDetector channelOrganisationGroupChangeDetector,
            IReleaseProviderPersistenceService releaseProviderPersistenceService,
            IProviderVersionReleaseService providerVersionReleaseService,
            IExistingReleasedProvidersForChannelFilterService existingReleasedProvidersForChannelFilterService,
            IProviderVersionToChannelReleaseService providerVersionToChannelReleaseService,
            IGenerateVariationReasonsForChannelService generateVariationReasonsForChannelService,
            IProviderVariationReasonsReleaseService providerVariationReasonsReleaseService,
            IPublishedProviderContentChannelPersistenceService publishedProviderContentChannelPersistenceService,
            IPublishedFundingContentsChannelPersistenceService publishedFundingContentsChannelPersistenceService,
            IFundingGroupProviderPersistenceService fundingGroupProviderPersistenceService,
            IFundingGroupService fundingGroupService,
            IFundingGroupDataGenerator fundingGroupDataGenerator,
            IFundingGroupDataPersistenceService fundingGroupDataPersistenceService,
            IReleaseManagementRepository releaseManagementRepository,
            ICurrentDateTime currentDateTime,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishProvidersLoadContext, nameof(publishProvidersLoadContext));
            Guard.ArgumentNotNull(providersForChannelFilterService, nameof(providersForChannelFilterService));
            Guard.ArgumentNotNull(channelOrganisationGroupGeneratorService, nameof(channelOrganisationGroupGeneratorService));
            Guard.ArgumentNotNull(channelOrganisationGroupChangeDetector, nameof(channelOrganisationGroupChangeDetector));
            Guard.ArgumentNotNull(releaseProviderPersistenceService, nameof(releaseProviderPersistenceService));
            Guard.ArgumentNotNull(providerVersionReleaseService, nameof(providerVersionReleaseService));
            Guard.ArgumentNotNull(existingReleasedProvidersForChannelFilterService, nameof(existingReleasedProvidersForChannelFilterService));
            Guard.ArgumentNotNull(providerVersionToChannelReleaseService, nameof(providerVersionToChannelReleaseService));
            Guard.ArgumentNotNull(generateVariationReasonsForChannelService, nameof(generateVariationReasonsForChannelService));
            Guard.ArgumentNotNull(providerVariationReasonsReleaseService, nameof(providerVariationReasonsReleaseService));
            Guard.ArgumentNotNull(publishedProviderContentChannelPersistenceService, nameof(publishedProviderContentChannelPersistenceService));
            Guard.ArgumentNotNull(publishedFundingContentsChannelPersistenceService, nameof(publishedFundingContentsChannelPersistenceService));
            Guard.ArgumentNotNull(fundingGroupService, nameof(fundingGroupService));
            Guard.ArgumentNotNull(fundingGroupProviderPersistenceService, nameof(fundingGroupProviderPersistenceService));
            Guard.ArgumentNotNull(fundingGroupDataGenerator, nameof(fundingGroupDataGenerator));
            Guard.ArgumentNotNull(fundingGroupDataPersistenceService, nameof(fundingGroupDataPersistenceService));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(currentDateTime, nameof(currentDateTime));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishProvidersLoadContext = publishProvidersLoadContext;
            _providersForChannelFilterService = providersForChannelFilterService;
            _channelOrganisationGroupGeneratorService = channelOrganisationGroupGeneratorService;
            _channelOrganisationGroupChangeDetector = channelOrganisationGroupChangeDetector;
            _releaseProviderPersistenceService = releaseProviderPersistenceService;
            _providerVersionReleaseService = providerVersionReleaseService;
            _existingReleasedProvidersForChannelFilterService = existingReleasedProvidersForChannelFilterService;
            _providerVersionToChannelReleaseService = providerVersionToChannelReleaseService;
            _generateVariationReasonsForChannelService = generateVariationReasonsForChannelService;
            _providerVariationReasonsReleaseService = providerVariationReasonsReleaseService;
            _publishedProviderContentChannelPersistenceService = publishedProviderContentChannelPersistenceService;
            _publishedFundingContentsChannelPersistenceService = publishedFundingContentsChannelPersistenceService;
            _fundingGroupProviderPersistenceService = fundingGroupProviderPersistenceService;
            _fundingGroupService = fundingGroupService;
            _fundingGroupDataGenerator = fundingGroupDataGenerator;
            _fundingGroupDataPersistenceService = fundingGroupDataPersistenceService;
            _repo = releaseManagementRepository;
            _currentDateTime = currentDateTime;
            _logger = logger;
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

            _logger.Information("Starting release to channels for channel '{ChannelCode}' with ID '{ChannelId}'", channel.ChannelCode, channel.ChannelId);
            _logger.Information("Releasing a total of '{Count}' providers to '{ChannelCode}'", batchPublishedProviderIds.Count(), channel.ChannelCode);

            DateTime currentDateTime = _currentDateTime.GetUtcNow();

            _logger.Information("Retrieving existing latest versions of providers in channel");
            IEnumerable<ProviderVersionInChannel> existingLatestVersionOfProvidersInChannel = await _repo.GetLatestPublishedProviderVersionsUsingAmbientTransaction(specification.Id, new[] { channel.ChannelId });
            _logger.Information($"Retrieved total of '{existingLatestVersionOfProvidersInChannel.Count()}' latest versions of providers in channel");

            _logger.Information("Retrieving current state of published providers for release to channel");
            IEnumerable<PublishedProvider> publishedProvidersBatch = await _publishProvidersLoadContext.GetOrLoadProviders(batchPublishedProviderIds);
            _logger.Information("A total of {Count} current state of published providers loaded for release to channel", publishedProvidersBatch.Count());

            _logger.Information("Filtering providers for channel '{ChannelCode}'", channel.ChannelCode);
            Dictionary<string, PublishedProviderVersion> providersToRelease = _providersForChannelFilterService.FilterProvidersForChannel(channel,
                publishedProvidersBatch.Select(_ => _.Released),
                fundingConfiguration)
                .ToDictionary(_ => _.ProviderId);
            _logger.Information("A total of '{Count}' providers can be released for channel '{ChannelCode}'", providersToRelease.Count, channel.ChannelCode);

            _logger.Information("Producing organisation groups for channel '{ChannelCode}'", channel.ChannelCode);
            IEnumerable<OrganisationGroupResult> allOrganisationGroups =
                await _channelOrganisationGroupGeneratorService.GenerateOrganisationGroups(channel,
                    fundingConfiguration,
                    specification,
                    providersToRelease.Values);
            _logger.Information("A total of {Count} organisation groups are generated for channel '{ChannelCode}'", allOrganisationGroups.Count(), channel.ChannelCode);

            _logger.Information("Determing which organisation groups to persist with new data for channel '{ChannelCode}'", channel.ChannelCode);
            IEnumerable<OrganisationGroupResult> organisationGroupsToCreate =
                await _channelOrganisationGroupChangeDetector.DetermineFundingGroupsToCreateBasedOnProviderVersions(allOrganisationGroups,
                    specification,
                    channel);
            _logger.Information("A total of '{Count}' new FundingGroups should be created for channel '{ChannelCode}'", organisationGroupsToCreate.Count(), channel.ChannelCode);

            _logger.Information("Retrieving published providers in groups to create for release to channel");
            IEnumerable<PublishedProvider> publishedProvidersInGroupsToCreate = await _publishProvidersLoadContext.GetOrLoadProviders(organisationGroupsToCreate
                    .SelectMany(_ => _.Providers)
                    .Select(_ => _.ProviderId)
                    .Distinct());
            _logger.Information("A total of {Count} published providers in groups to create loaded for release to channel", publishedProvidersInGroupsToCreate.Count());

            _logger.Information("Retrieving released provider versions in groups to create for release to channel");
            Dictionary<string, PublishedProviderVersion> providersInGroupsToCreate = publishedProvidersInGroupsToCreate.Select(_ => _.Released).ToDictionary(_ => _.ProviderId);
            _logger.Information("A total of {Count} released provider versions in groups to create loaded for release to channel", providersInGroupsToCreate.Count());

            _logger.Information("Creating funding groups for channel '{ChannelCode}'", channel.ChannelCode);
            IEnumerable<FundingGroup> fundingGroups =
                await _fundingGroupService.CreateFundingGroups(specification.Id, channel.ChannelId, organisationGroupsToCreate);
            _logger.Information("Created a total of '{Count}' new funding groups in channel '{ChannelCode}'", fundingGroups.Count(), channel.ChannelCode);

            _logger.Information("Generating funding group data (versions) for channel '{ChannelCode}'", channel.ChannelCode);
            IEnumerable<GeneratedPublishedFunding> fundingGroupData =
                await _fundingGroupDataGenerator.Generate(organisationGroupsToCreate,
                                                          specification,
                                                          channel,
                                                          providersInGroupsToCreate.Keys,
                                                          author,
                                                          jobId,
                                                          correlationId);

            _logger.Information("Persisting a total of '{Count}' funding group versions for channel '{ChannelCode}'", fundingGroupData.Count(), channel.ChannelCode);
            IEnumerable<FundingGroupVersion> fundingGroupVersionsCreated = await _fundingGroupDataPersistenceService.ReleaseFundingGroupData(fundingGroupData, channel.ChannelId);

            IEnumerable<PublishedProviderVersion> providersInGroupsToRelease = organisationGroupsToCreate
                .SelectMany(_ => _.Providers)
                .Select(_ => _.ProviderId)
                .Distinct()
                .Select(_ => providersInGroupsToCreate[_]);
            _logger.Information("A total count of providers in all new funding groups of '{Count}' were generated for channel '{ChannelCode}'", fundingGroupData.Count(), channel.ChannelCode);


            IEnumerable<PublishedProviderVersion> providersToReleaseInBatch = _existingReleasedProvidersForChannelFilterService.FilterExistingReleasedProviderInChannel(providersInGroupsToRelease,
                existingLatestVersionOfProvidersInChannel,
                batchPublishedProviderIds,
                channel.ChannelId,
                specification.Id);
            _logger.Information("Releasing a total of '{Count}' providers for channel '{ChannelCode}'", providersToReleaseInBatch.Count(), channel.ChannelCode);

            IEnumerable<string> providersInGroupsToReleasedProviderIds = providersToReleaseInBatch.Select(_ => _.ProviderId);

            _logger.Information("Persisting ReleasedProviders for channel '{ChannelCode}'", channel.ChannelCode);
            IEnumerable<ReleasedProvider> releasedProviders = await _releaseProviderPersistenceService.ReleaseProviders(
                providersInGroupsToReleasedProviderIds,
                specification.Id);

            _logger.Information("Persisting ReleasedProviderVersions for channel '{ChannelCode}'", channel.ChannelCode);
            await _providerVersionReleaseService.ReleaseProviderVersions(
                providersToReleaseInBatch,
                specification.Id);

            _logger.Information("Persisting released provider versions in channel for channel '{ChannelCode}'", channel.ChannelCode);
            await _providerVersionToChannelReleaseService.ReleaseProviderVersionChannel(
                providersInGroupsToReleasedProviderIds,
                channel.ChannelId,
                currentDateTime);

            _logger.Information("Generating ReleasedProvider variations for channel '{ChannelCode}'", channel.ChannelCode);
            IDictionary<string, IEnumerable<VariationReason>> variationReasonsForProviders = await _generateVariationReasonsForChannelService.GenerateVariationReasonsForProviders(
                providersInGroupsToReleasedProviderIds,
                existingLatestVersionOfProvidersInChannel,
                channel,
                specification,
                fundingConfiguration,
                allOrganisationGroups.GroupByProviderId());

            int totalVariations = variationReasonsForProviders.Values.Select(_ => _.Count()).Sum();

            _logger.Information("Generated a total of '{Count}' providers with a total of '{totalVariations}' variations for channel '{ChannelCode}'", variationReasonsForProviders.Count, totalVariations, channel.ChannelCode);

            _logger.Information("Persisting released provider variations for channel '{ChannelCode}'", channel.ChannelCode);
            await _providerVariationReasonsReleaseService.PopulateReleasedProviderChannelVariationReasons(variationReasonsForProviders, channel);

            _logger.Information("Persisting funding group providers for channel '{ChannelCode}'", channel.ChannelCode);
            await _fundingGroupProviderPersistenceService.PersistFundingGroupProviders(channel.ChannelId, fundingGroupData, providersInGroupsToRelease);

            _logger.Information("Persisting released provider blob document contents for channel '{ChannelCode}'", channel.ChannelCode);
            await _publishedProviderContentChannelPersistenceService
                .SavePublishedProviderContents(specification, providersToReleaseInBatch, channel, variationReasonsForProviders);

            _logger.Information("Persisting funding group blob document contents for channel '{ChannelCode}'", channel.ChannelCode);
            await _publishedFundingContentsChannelPersistenceService
                .SavePublishedFundingContents(fundingGroupData.Select(_ => _.PublishedFundingVersion), channel);

            _logger.Information("Completed release for channel '{ChannelCode}'", channel.ChannelCode);
        }
    }
}
