using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Migration;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.SqlExport;
using Polly;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class PublishedFundingReleaseManagementMigrator : IPublishedFundingReleaseManagementMigrator
    {
        private const int CosmosBatchSize = 200;
        private const int BlobClientThrottleCount = 50;

        private readonly IPublishedFundingRepository _cosmosRepo;
        private readonly IPublishedFundingBulkRepository _publishedFundingBulkRepository;
        private readonly IReleaseManagementMigrationCosmosProducerConsumer<IdPartitionKeyLookup> _fundingMigrator;
        private readonly IReleaseManagementMigrationCosmosProducerConsumer<IdPartitionKeyLookup> _providerMigrator;
        private readonly IBlobClient _blobClient;
        private readonly IReleaseManagementDataTableImporter _dataTableImporter;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _blobPolicy;

        /// <summary>
        /// Key is "{channelId}_{Funding.FundingId}"
        /// </summary>
        private readonly ConcurrentDictionary<string, PublishedFundingVersion> _publishedFundingVersions = new ConcurrentDictionary<string, PublishedFundingVersion>();
        /// <summary>
        /// Key is "{providerId}"
        /// </summary>
        private readonly ConcurrentDictionary<string, PublishedProviderVersion> _publishedProviderVersions = new ConcurrentDictionary<string, PublishedProviderVersion>();
        /// <summary>
        /// Key is "{channelId}_{Funding.FundingId}"
        /// </summary>
        private readonly ConcurrentDictionary<string, FundingGroupVersion> _fundingGroupVersions = new ConcurrentDictionary<string, FundingGroupVersion>();
        /// <summary>
        /// Key is "{releasedProviderId}"
        /// </summary>
        private readonly ConcurrentDictionary<Guid, ReleasedProvider> _releasedProvidersById = new ConcurrentDictionary<Guid, ReleasedProvider>();
        /// <summary>
        /// Key is "{providerId}_{specificationId}"
        /// </summary>
        private readonly ConcurrentDictionary<string, ReleasedProvider> _releasedProviders = new ConcurrentDictionary<string, ReleasedProvider>();
        /// <summary>
        /// Key is "{Provider.FundingId}"
        /// </summary>
        private readonly ConcurrentDictionary<string, ReleasedProviderVersion> _releasedProviderVersions = new ConcurrentDictionary<string, ReleasedProviderVersion>();
        /// <summary>
        /// Key is "{channelId}_{specificationId}_{groupingReasonId}_{organisationGroupTypeClassification}_{organisationGroupIdentifierValue}"
        /// </summary>
        private readonly ConcurrentDictionary<string, FundingGroup> _fundingGroups = new ConcurrentDictionary<string, FundingGroup>();

        private readonly ConcurrentDictionary<string, FundingGroupVersion> _createFundingGroupVersions = new ConcurrentDictionary<string, FundingGroupVersion>();
        private readonly ConcurrentDictionary<string, FundingGroupVersionVariationReason> _createFundingGroupVariationReasons = new ConcurrentDictionary<string, FundingGroupVersionVariationReason>();
        private readonly ConcurrentBag<BlobToMigrate> _blobsToMigrate = new ConcurrentBag<BlobToMigrate>();

        private class BlobToMigrate
        {
            public BlobToMigrate(string sourceContainer, string sourceFileName, string targetContainer, string targetFileName)
            {
                SourceContainer = sourceContainer;
                SourceFileName = sourceFileName;
                TargetContainer = targetContainer;
                TargetFileName = targetFileName;
            }

            public string SourceContainer { get; }
            public string SourceFileName { get; }
            public string TargetContainer { get; }
            public string TargetFileName { get; }
        }

        public PublishedFundingReleaseManagementMigrator(IPublishedFundingRepository publishedFundingRepository,
            IPublishedFundingBulkRepository publishedFundingBulkRepository,
            IReleaseManagementMigrationCosmosProducerConsumer<IdPartitionKeyLookup> fundingMigrator,
            IReleaseManagementMigrationCosmosProducerConsumer<IdPartitionKeyLookup> providerMigrator,
            IBlobClient blobClient,
            IReleaseManagementDataTableImporter dataTableImporter,
            ILogger logger,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingBulkRepository, nameof(publishedFundingBulkRepository));
            Guard.ArgumentNotNull(fundingMigrator, nameof(fundingMigrator));
            Guard.ArgumentNotNull(providerMigrator, nameof(providerMigrator));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(dataTableImporter, nameof(dataTableImporter));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));

            _cosmosRepo = publishedFundingRepository;
            _publishedFundingBulkRepository = publishedFundingBulkRepository;
            _fundingMigrator = fundingMigrator;
            _providerMigrator = providerMigrator;
            _blobClient = blobClient;
            _dataTableImporter = dataTableImporter;
            _logger = logger;
            _blobPolicy = publishingResiliencePolicies.BlobClient;
        }

        public async Task Migrate(Dictionary<string, FundingStream> fundingStreams,
            Dictionary<string, FundingPeriod> fundingPeriods,
            Dictionary<string, Channel> channels,
            Dictionary<string, SqlModels.GroupingReason> groupingReasons,
            Dictionary<string, SqlModels.VariationReason> variationReasons,
            Dictionary<string, Specification> specifications)
        {
            IDataTableImporter dataImporter = _dataTableImporter as IDataTableImporter;

            var fundingPeriodId = fundingPeriods.Count() == 1 ? fundingPeriods.Keys.FirstOrDefault() : null;

            _logger.Information("Loading PublishedFundingVersions for migration");
            await _fundingMigrator.RunAsync(fundingStreams, fundingPeriods, channels,
                groupingReasons, variationReasons, specifications,
                _cosmosRepo.GetPublishedFundingVersionDocumentIdIterator(CosmosBatchSize, fundingStreams.Keys.ToArray(), fundingPeriodId), ProcessPublishedFundingVersions);
            _logger.Information($"Loaded '{_publishedFundingVersions.Count}' PublishedFundingVersions for migration");

            _logger.Information("Loading PublishedProviderVersions for migration");
            await _providerMigrator.RunAsync(fundingStreams, fundingPeriods, channels,
                groupingReasons, variationReasons, specifications,
                _cosmosRepo.GetReleasedPublishedProviderVersionIdIterator(CosmosBatchSize, fundingStreams.Keys.ToArray(), fundingPeriodId), ProcessPublishedProviderVersions);
            _logger.Information($"Loaded '{_publishedProviderVersions.Count}' PublishedProviderVersions for migration");

            DetectMissingPublishedProviderVersionRecordsFromFundingGroups();

            FundingGroupDataTableBuilder fundingGroupsBuilder = new FundingGroupDataTableBuilder();
            fundingGroupsBuilder.AddRows(_fundingGroups.Values.ToArray());
            _logger.Information($"Importing {_fundingGroups.Values.Count} FundingGroups");
            await dataImporter.ImportDataTable(fundingGroupsBuilder, SqlBulkCopyOptions.KeepIdentity);

            FundingGroupVersionDataTableBuilder fundingGroupVersionsBuilder = new FundingGroupVersionDataTableBuilder();
            fundingGroupVersionsBuilder.AddRows(_createFundingGroupVersions.Values.ToArray());
            _logger.Information($"Importing {_createFundingGroupVersions.Count} FundingGroupVersions");
            await dataImporter.ImportDataTable(fundingGroupVersionsBuilder, SqlBulkCopyOptions.KeepIdentity);

            FundingGroupVersionVariationReasonsDataTableBuilder fundingVariationReasonsBuilder = new FundingGroupVersionVariationReasonsDataTableBuilder();
            fundingVariationReasonsBuilder.AddRows(_createFundingGroupVariationReasons.Values.ToArray());
            _logger.Information($"Importing {_createFundingGroupVariationReasons.Count} FundingGroupVariationReasons");
            await dataImporter.ImportDataTable(fundingVariationReasonsBuilder, SqlBulkCopyOptions.KeepIdentity);

            ReleasedProviderDataTableBuilder releasedProvidersBuilder = new ReleasedProviderDataTableBuilder();
            releasedProvidersBuilder.AddRows(_releasedProviders.Values.ToArray());
            _logger.Information($"Importing {_releasedProviders.Count} ReleasedProviders");
            await dataImporter.ImportDataTable(releasedProvidersBuilder, SqlBulkCopyOptions.KeepIdentity);

            ReleasedProviderVersionDataTableBuilder releasedProviderVersionsBuilder = new ReleasedProviderVersionDataTableBuilder();
            releasedProviderVersionsBuilder.AddRows(_releasedProviderVersions.Values.ToArray());
            _logger.Information($"Importing {_releasedProviderVersions.Count} ReleasedProviderVersions");
            await dataImporter.ImportDataTable(releasedProviderVersionsBuilder, SqlBulkCopyOptions.KeepIdentity);

            _logger.Information("Loading PopulateLinkingTables for migration");
            await PopulateLinkingTables(channels, variationReasons);
            _logger.Information("Completed PopulateLinkingTables for migration");

            _logger.Information("Loading MigrateBlobs to Container for migration");
            await MigrateBlobs();
            _logger.Information("Completed MigrateBlobs for migration");
        }

        private void DetectMissingPublishedProviderVersionRecordsFromFundingGroups()
        {
            IEnumerable<string> allProviderFundingIdFromGroups = _publishedFundingVersions.Values.SelectMany(_ => _.ProviderFundings).Distinct();

            IEnumerable<string> allProviderFundingIds = _publishedProviderVersions.Values.Select(_ => _.FundingId);

            ILookup<string, IGrouping<string, string>> allProviderFundingGrouping = allProviderFundingIds
                .GroupBy(_ => _[.._.LastIndexOf('-')])
                .ToLookup(l => l.Key);

            string[] missingProviderVersions = allProviderFundingIdFromGroups.Except(allProviderFundingIds).ToArray();

            if (missingProviderVersions.AnyWithNullCheck())
            {
                IEnumerable<string> missingFunding = missingProviderVersions.Where(_ =>
                {
                    // i.e this will return DSG-FY-2021-10004000 for provider version DSG-FY-2021-10004000-8_0
                    string missingFunding = _[.._.LastIndexOf('-')];
                    // i.e this will return 8 for provider version DSG-FY-2021-10004000-8_0
                    int missingVersion = Convert.ToInt16(_.Split('-')[4].Split('_')[0]);

                    // check to see if there are no provider versions with a higher version than the missing version
                    return !allProviderFundingGrouping.Contains(missingFunding) || allProviderFundingGrouping[missingFunding].AnyWithNullCheck(fundingGroup =>
                    {
                        int existingProviderVersion = fundingGroup.Select(_ => Convert.ToInt16(_.Split('-')[4].Split('_')[0])).MaxBy(_ => _);
                        return existingProviderVersion < missingVersion;
                    });
                });

                if (missingFunding.AnyWithNullCheck())

                {
                    // don't throw an exception instead log that there are missing versions and move on
                    throw new InvalidOperationException("The following PublishedProviderVersions are missing in cosmos: " + string.Join(", ", missingFunding));
                }
            }
        }

        private static string GetFundingGroupDictionaryKey(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue)
        {
            return $"{channelId}_{specificationId}_{groupingReasonId}_{organisationGroupTypeClassification}_{organisationGroupIdentifierValue}";
        }

        protected async Task ProcessPublishedFundingVersions(CancellationToken cancellationToken,
            dynamic context,
            ArraySegment<IdPartitionKeyLookup> publishedFundingVersionIds)
        {
            IReleaseManagementImportContext ctx = ((IReleaseManagementImportContext)context);

            IEnumerable<PublishedFundingVersion> publishedFunding = await _publishedFundingBulkRepository.GetPublishedFundingVersions(publishedFundingVersionIds.Select(_ => new KeyValuePair<string, string>(_.Id, _.ParitionKey)));

            foreach (PublishedFundingVersion fundingVersion in publishedFunding)
            {
                GenerateFundingGroupsAndVersions(fundingVersion, ctx);
            }
        }

        private void GenerateFundingGroupsAndVersions(PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            IEnumerable<Channel> channels = GetEligibleChannelsForExistingFundingVersion(fundingVersion.GroupingReason, ctx.Channels);

            foreach (Channel channel in channels)
            {
                FundingGroup fundingGroup = CreateFundingGroup(channel.ChannelId, fundingVersion, ctx);
                GenerateFundingGroupVersion(channel, fundingGroup, fundingVersion, ctx);

                MigrateBlob(fundingVersion, channel.ChannelCode);
            }
        }

        private IEnumerable<Channel> GetEligibleChannelsForExistingFundingVersion(
            CalculateFunding.Models.Publishing.GroupingReason groupingReason,
            Dictionary<string, Channel> allChannels)
        {
            List<Channel> channels = new List<Channel>(2);

            // Always publish for statement for now
            channels.Add(allChannels["Statement"]);

            // Always publish for spec to spec for now
            channels.Add(allChannels["SpecToSpec"]);

            switch (groupingReason)
            {
                case CalculateFunding.Models.Publishing.GroupingReason.Contracting:
                    {
                        channels.Add(allChannels["Contracting"]);
                        break;
                    }

                case CalculateFunding.Models.Publishing.GroupingReason.Payment:
                    {
                        channels.Add(allChannels["Payment"]);
                        break;
                    }
            }

            return channels;
        }

        private FundingGroup CreateFundingGroup(int channelId, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            int groupingReasonId = ctx.GroupingReasons[fundingVersion.GroupingReason.ToString()].GroupingReasonId;

            string key = GetFundingGroupDictionaryKey(channelId, fundingVersion.SpecificationId, groupingReasonId, fundingVersion.OrganisationGroupTypeCode, fundingVersion.OrganisationGroupIdentifierValue);

            if (!_fundingGroups.TryGetValue(key, out var fundingGroup))
            {
                fundingGroup = new FundingGroup()
                {
                    FundingGroupId = Guid.NewGuid(),
                    ChannelId = channelId,
                    GroupingReasonId = groupingReasonId,
                    OrganisationGroupIdentifierValue = fundingVersion.OrganisationGroupIdentifierValue,
                    OrganisationGroupName = fundingVersion.OrganisationGroupName,
                    OrganisationGroupSearchableName = fundingVersion.OrganisationGroupSearchableName,
                    OrganisationGroupTypeClassification = fundingVersion.OrganisationGroupTypeClassification,
                    OrganisationGroupTypeCode = fundingVersion.OrganisationGroupTypeCode,
                    OrganisationGroupTypeIdentifier = fundingVersion.OrganisationGroupTypeIdentifier,
                    SpecificationId = fundingVersion.SpecificationId,
                };

                if (!_fundingGroups.TryAdd(key, fundingGroup))
                {
                    return _fundingGroups[key];
                }
            }

            return fundingGroup;
        }

        private FundingGroupVersion GenerateFundingGroupVersion(Channel channel, FundingGroup fundingGroup, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            FundingGroupVersion fundingGroupVersion = new FundingGroupVersion()
            {
                FundingGroupVersionId = Guid.NewGuid(),
                ChannelId = channel.ChannelId,
                CorrelationId = fundingVersion.CorrelationId ?? ctx.JobId,
                FundingGroupId = fundingGroup.FundingGroupId,
                FundingId = fundingVersion.FundingId,
                FundingPeriodId = ctx.FundingPeriods[fundingVersion.FundingPeriod.Id].FundingPeriodId,
                FundingStreamId = ctx.FundingStreams[fundingVersion.FundingStreamId].FundingStreamId,
                GroupingReasonId = ctx.GroupingReasons[fundingVersion.GroupingReason.ToString()].GroupingReasonId,
                JobId = fundingVersion.JobId ?? ctx.JobId,
                MajorVersion = fundingVersion.MajorVersion,
                MinorVersion = fundingVersion.MinorVersion,
                SchemaVersion = fundingVersion.SchemaVersion,
                StatusChangedDate = fundingVersion.StatusChangedDate,
                TemplateVersion = fundingVersion.TemplateVersion,
                TotalFunding = fundingVersion.TotalFunding ?? 0,
                EarliestPaymentAvailableDate = fundingVersion.EarliestPaymentAvailableDate,
                ExternalPublicationDate = fundingVersion.ExternalPublicationDate,
            };

            string fundingGroupKey = $"{fundingVersion.FundingStreamId}-{fundingVersion.FundingPeriod.Id}-{fundingVersion.GroupingReason}-{fundingVersion.OrganisationGroupTypeCode}-{fundingVersion.OrganisationGroupIdentifierValue}-{fundingVersion.MajorVersion}-{channel.ChannelId}";

            if (!_createFundingGroupVersions.TryAdd(fundingGroupKey, fundingGroupVersion))
            {
                throw new InvalidOperationException($"Unable to insert duplicate funding group: '{fundingGroupKey}'");
            }

            if (!_fundingGroupVersions.TryAdd($"{channel.ChannelId}_{fundingGroupVersion.FundingId}", fundingGroupVersion))
            {
                throw new InvalidOperationException($"Funding group version already exists for ID {fundingGroupVersion.FundingId} in channel {channel.ChannelCode}");
            }

            if (!_publishedFundingVersions.TryAdd($"{channel.ChannelId}_{fundingVersion.FundingId}", fundingVersion))
            {
                throw new InvalidOperationException($"Published funding versions exists for ID {fundingGroupVersion.FundingId} in channel {channel.ChannelCode}");
            }

            PopulateVariationReasonsForFundingGroupVersion(fundingGroupVersion, fundingVersion, ctx);

            return fundingGroupVersion;
        }

        private void PopulateVariationReasonsForFundingGroupVersion(FundingGroupVersion fundingGroupVersion, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            if (fundingVersion.VariationReasons.AnyWithNullCheck())
            {
                foreach (CalculateFunding.Models.Publishing.VariationReason variationReason in fundingVersion.VariationReasons)
                {
                    if (ctx.VariationReasons.TryGetValue(variationReason.ToString(), out SqlModels.VariationReason value))
                    {
                        FundingGroupVersionVariationReason reason = new FundingGroupVersionVariationReason()
                        {
                            FundingGroupVersionVariationReasonId = Guid.NewGuid(),
                            FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                            VariationReasonId = value.VariationReasonId,
                        };

                        _createFundingGroupVariationReasons.TryAdd($"{reason.FundingGroupVersionId}-{value.VariationReasonId}", reason);
                    }
                }
            }

            int fundingUpdatedVariationReasonId = ctx.VariationReasons["FundingUpdated"].VariationReasonId;

            string fundingGroupVersionFundingUpdatedVariationReasonKey = $"{fundingGroupVersion.FundingGroupVersionId}-{fundingUpdatedVariationReasonId}";

            if (fundingGroupVersion.MajorVersion == 1 && !_createFundingGroupVariationReasons.ContainsKey(fundingGroupVersionFundingUpdatedVariationReasonKey))
            {
                _createFundingGroupVariationReasons.TryAdd(fundingGroupVersionFundingUpdatedVariationReasonKey, new FundingGroupVersionVariationReason()
                {
                    FundingGroupVersionVariationReasonId = Guid.NewGuid(),
                    FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                    VariationReasonId = ctx.VariationReasons["FundingUpdated"].VariationReasonId,
                });
            }

            int profilingUpdatedVariationReasonId = ctx.VariationReasons["ProfilingUpdated"].VariationReasonId;

            string fundingGroupVersionProfilingUpdatedVariationReasonKey = $"{fundingGroupVersion.FundingGroupVersionId}-{profilingUpdatedVariationReasonId}";

            if (fundingGroupVersion.MajorVersion == 1 && !_createFundingGroupVariationReasons.ContainsKey(fundingGroupVersionProfilingUpdatedVariationReasonKey))
            {
                _createFundingGroupVariationReasons.TryAdd(fundingGroupVersionProfilingUpdatedVariationReasonKey, new FundingGroupVersionVariationReason()
                {
                    FundingGroupVersionVariationReasonId = Guid.NewGuid(),
                    FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                    VariationReasonId = ctx.VariationReasons["ProfilingUpdated"].VariationReasonId,
                });
            }
        }

        protected async Task ProcessPublishedProviderVersions(CancellationToken cancellationToken,
            dynamic context,
            ArraySegment<IdPartitionKeyLookup> publishedProviderVersionIds)
        {
            IEnumerable<PublishedProviderVersion> publishedProviders = await _publishedFundingBulkRepository.GetPublishedProviderVersions(publishedProviderVersionIds.Select(_ => new KeyValuePair<string, string>(_.Id, _.ParitionKey)));

            foreach (PublishedProviderVersion providerVersion in publishedProviders)
            {
                if (!_publishedProviderVersions.TryAdd($"{providerVersion.ProviderId}_{providerVersion.SpecificationId}_{providerVersion.MajorVersion}_{providerVersion.MinorVersion}", providerVersion))
                {
                    if (providerVersion.FundingStreamId != "PSG" && providerVersion.FundingPeriodId == "AY-1920")
                    {
                        _logger.Warning($"Duplicate published provider version found for {providerVersion.FundingStreamId}-{providerVersion.FundingPeriodId}-{providerVersion.ProviderId}_{providerVersion.MajorVersion}_{providerVersion.MinorVersion} in specification {providerVersion.SpecificationId}");
                    }
                }

                GenerateReleasedProvidersAndVersions(providerVersion);
            }
        }

        private void GenerateReleasedProvidersAndVersions(PublishedProviderVersion providerVersion)
        {
            string releasedProviderKey = $"{providerVersion.ProviderId}_{providerVersion.SpecificationId}";
            ReleasedProvider releasedProvider = CreateReleasedProvider(providerVersion, releasedProviderKey);

            if (!_releasedProviderVersions.ContainsKey(providerVersion.FundingId))
            {
                CreateReleasedProviderVersion(releasedProvider, providerVersion);
            }
        }

        private ReleasedProvider CreateReleasedProvider(PublishedProviderVersion providerVersion, string releasedProviderKey)
        {
            if (!_releasedProviders.TryGetValue(releasedProviderKey, out var releasedProvider))
            {
                releasedProvider = new ReleasedProvider
                {
                    ReleasedProviderId = Guid.NewGuid(),
                    SpecificationId = providerVersion.SpecificationId,
                    ProviderId = providerVersion.ProviderId
                };

                if (_releasedProviders.TryAdd(releasedProviderKey, releasedProvider))
                {
                    _releasedProvidersById.AddOrUpdate(releasedProvider.ReleasedProviderId, releasedProvider, (id, existing) => releasedProvider);
                }
                else
                {
                    return _releasedProviders[releasedProviderKey];
                }
            }

            return releasedProvider;
        }

        private ReleasedProviderVersion CreateReleasedProviderVersion(ReleasedProvider releasedProvider, PublishedProviderVersion providerVersion)
        {
            ReleasedProviderVersion releasedProviderVersion = new ReleasedProviderVersion
            {
                ReleasedProviderVersionId = Guid.NewGuid(),
                ReleasedProviderId = releasedProvider.ReleasedProviderId,
                MajorVersion = providerVersion.MajorVersion,
                MinorVersion = providerVersion.MinorVersion,
                FundingId = providerVersion.FundingId,
                TotalFunding = providerVersion.TotalFunding ?? 0m,
                CoreProviderVersionId = providerVersion.Provider.ProviderVersionId,
            };

            _releasedProviderVersions.AddOrUpdate(releasedProviderVersion.FundingId, releasedProviderVersion, (id, existing) => releasedProviderVersion);

            return releasedProviderVersion;
        }

        private async Task PopulateLinkingTables(Dictionary<string, Channel> channels, Dictionary<string, SqlModels.VariationReason> variationReasons)
        {
            ConcurrentDictionary<string, ReleasedProviderVersionChannel> createReleasedProviderVersionChannels = new ConcurrentDictionary<string, ReleasedProviderVersionChannel>();
            List<FundingGroupProvider> createFundingGroupProviders = new List<FundingGroupProvider>();
            List<ReleasedProviderChannelVariationReason> createReleasedProviderChannelVariationReasons = new List<ReleasedProviderChannelVariationReason>();

            foreach (KeyValuePair<string, PublishedFundingVersion> publishedFunding in _publishedFundingVersions)
            {
                string channelId = publishedFunding.Key.Split('_')[0];
                string fundingVersionFundingId = publishedFunding.Value.FundingId;

                _fundingGroupVersions.TryGetValue($"{channelId}_{fundingVersionFundingId}", out FundingGroupVersion fundingGroupVersion);

                if (fundingGroupVersion == null)
                {
                    throw new KeyNotFoundException($"FundingGroupVersion not found for fundingId '{fundingVersionFundingId}'. PublishedFundingVersion: {publishedFunding.Value.Id}");
                }

                foreach (string providerVersionFundingId in publishedFunding.Value.ProviderFundings)
                {
                    _releasedProviderVersions.TryGetValue(providerVersionFundingId, out ReleasedProviderVersion releasedProviderVersion);

                    if (releasedProviderVersion == null)
                    {
                        // if we get here then we are ok to skip this grouping as we do a check further up
                        continue;
                    }

                    ReleasedProvider releasedProvider = _releasedProvidersById[releasedProviderVersion.ReleasedProviderId];
                    PublishedProviderVersion publishedProviderVersion = _publishedProviderVersions[$"{releasedProvider.ProviderId}_{releasedProvider.SpecificationId}_{releasedProviderVersion.MajorVersion}_{releasedProviderVersion.MinorVersion}"];

                    ReleasedProviderVersionChannel releasedProviderVersionChannel = new ReleasedProviderVersionChannel
                    {
                        ReleasedProviderVersionChannelId = Guid.NewGuid(),
                        ReleasedProviderVersionId = releasedProviderVersion.ReleasedProviderVersionId,
                        ChannelId = fundingGroupVersion.ChannelId,
                        StatusChangedDate = publishedProviderVersion.Date.UtcDateTime,
                        AuthorId = publishedProviderVersion.Author.Id,
                        AuthorName = publishedProviderVersion.Author.Name,
                    };

                    releasedProviderVersionChannel = createReleasedProviderVersionChannels.AddOrUpdate($"{releasedProviderVersionChannel.ChannelId}_{releasedProviderVersionChannel.ReleasedProviderVersionId}",
                        releasedProviderVersionChannel,
                        (id, existing) => { return existing; }
                    );

                    FundingGroupProvider fundingGroupProvider = new FundingGroupProvider
                    {
                        FundingGroupProviderId = Guid.NewGuid(),
                        FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                        ReleasedProviderVersionChannelId = releasedProviderVersionChannel.ReleasedProviderVersionChannelId
                    };

                    createFundingGroupProviders.Add(fundingGroupProvider);

                    if (publishedProviderVersion.VariationReasons.AnyWithNullCheck())
                    {
                        foreach (CalculateFunding.Models.Publishing.VariationReason variationReason in publishedProviderVersion.VariationReasons)
                        {
                            if (variationReasons.TryGetValue(variationReason.ToString(), out SqlModels.VariationReason value))
                            {
                                ReleasedProviderChannelVariationReason reason = new ReleasedProviderChannelVariationReason()
                                {
                                    ReleasedProviderChannelVariationReasonId = Guid.NewGuid(),
                                    ReleasedProviderVersionChannelId = releasedProviderVersionChannel.ReleasedProviderVersionChannelId,
                                    VariationReasonId = value.VariationReasonId,
                                };

                                createReleasedProviderChannelVariationReasons.Add(reason);
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Variation reason '{variationReason}' does not exist. PublishedProviderVersion: {publishedProviderVersion.Id}");
                            }
                        }
                    }

                    MigrateBlob(publishedProviderVersion, channels.Values.Single(_ => _.ChannelId == fundingGroupVersion.ChannelId).ChannelCode);
                }
            }

            IDataTableImporter importer = _dataTableImporter as IDataTableImporter;

            ReleasedProviderVersionChannelDataTableBuilder releasedChannelBuilder = new ReleasedProviderVersionChannelDataTableBuilder();
            ReleasedProviderChannelVariationReasonDataTableBuilder providerVariationReasonsBuilder = new ReleasedProviderChannelVariationReasonDataTableBuilder();
            FundingGroupProviderDataTableBuilder fundingGroupProviderBuilder = new FundingGroupProviderDataTableBuilder();

            releasedChannelBuilder.AddRows(createReleasedProviderVersionChannels.Values.ToArray());
            _logger.Information($"Importing {createReleasedProviderVersionChannels.Count} ReleasedProviderVersionChannels");
            await importer.ImportDataTable(releasedChannelBuilder, SqlBulkCopyOptions.KeepIdentity);

            fundingGroupProviderBuilder.AddRows(createFundingGroupProviders.ToArray());
            _logger.Information($"Importing {createFundingGroupProviders.Count} FundingGroupProviders");
            await importer.ImportDataTable(fundingGroupProviderBuilder, SqlBulkCopyOptions.KeepIdentity);

            providerVariationReasonsBuilder.AddRows(createReleasedProviderChannelVariationReasons.ToArray());
            _logger.Information($"Importing {createReleasedProviderChannelVariationReasons.Count} ReleasedProviderChannelVariationReasons");
            await importer.ImportDataTable(providerVariationReasonsBuilder, SqlBulkCopyOptions.KeepIdentity);
        }

        private async Task MigrateBlobs()
        {
            SemaphoreSlim throttle = new SemaphoreSlim(BlobClientThrottleCount);
            List<Task> trackedTasks = new List<Task>(_blobsToMigrate.Count);
            _logger.Information("Initiating MigrateBlobs for Copy blobs to Container");
            foreach (BlobToMigrate blob in _blobsToMigrate)
            {
                await throttle.WaitAsync();

                trackedTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await _blobPolicy.ExecuteAsync(() => _blobClient.StartCopyFromUriAsync(blob.SourceContainer, blob.SourceFileName, blob.TargetContainer, blob.TargetFileName));
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Failed to copy blob '{blob.SourceFileName}' to new {blob.TargetContainer}";

                        _logger.Error(ex, errorMessage);

                        throw;
                    }
                    finally
                    {
                        throttle.Release();
                    }
                }));
            }
            _logger.Information("Initiating WhenAllAndThrow task for Copy blobs to Container");
            await TaskHelper.WhenAllAndThrow(trackedTasks.ToArray());
        }

        private void MigrateBlob(PublishedFundingVersion publishedFundingVersion, string channelCode)
        {
            const string sourceContainer = "publishedfunding";
            const string targetContainer = "releasedgroups";
            string sourceBlobName = $"{publishedFundingVersion.FundingStreamId}-{publishedFundingVersion.FundingPeriod.Id}-{publishedFundingVersion.GroupingReason}-{publishedFundingVersion.OrganisationGroupTypeCode}-{publishedFundingVersion.OrganisationGroupIdentifierValue}-{publishedFundingVersion.MajorVersion}_{publishedFundingVersion.MinorVersion}.json";
            string targetBlobName = $"{channelCode}/{sourceBlobName}";

            _blobsToMigrate.Add(new BlobToMigrate(sourceContainer, sourceBlobName, targetContainer, targetBlobName));
        }

        private void MigrateBlob(PublishedProviderVersion publishedProviderVersion, string channelCode)
        {
            const string sourceContainer = "publishedproviderversions";
            const string targetContainer = "releasedproviders";
            string sourceBlobName = $"{publishedProviderVersion.FundingStreamId}-{publishedProviderVersion.FundingPeriodId}-{publishedProviderVersion.ProviderId}-{publishedProviderVersion.MajorVersion}_{publishedProviderVersion.MinorVersion}.json";
            string targetBlobName = $"{channelCode}/{sourceBlobName}";

            _blobsToMigrate.Add(new BlobToMigrate(sourceContainer, sourceBlobName, targetContainer, targetBlobName));
        }
    }
}
