using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Migration;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.SqlExport;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class PublishedFundingReleaseManagementMigrator : IPublishedFundingReleaseManagementMigrator
    {
        private const int CosmosBatchSize = 100;
        private const int BlobClientThrottleCount = 50;

        private int _nextFundingGroupId = 1;
        private int _nextFundingGroupVersionId = 1;
        private int _nextFundingGroupVersionVariationReasonId = 1;
        private int _nextReleasedProviderId = 1;
        private int _nextReleasedProviderVersionId = 1;
        private int _nextReleasedProviderChannelVariationReasonsId = 1;
        private int _nextReleasedProviderVersionChannelId = 1;
        private int _nextFundingGroupProviderId = 1;

        private readonly IPublishedFundingRepository _cosmosRepo;
        private readonly IReleaseManagementMigrationCosmosProducerConsumer<PublishedFundingVersion> _fundingMigrator;
        private readonly IReleaseManagementMigrationCosmosProducerConsumer<PublishedProviderVersion> _providerMigrator;
        private readonly IBlobClient _blobClient;
        private readonly IReleaseManagementDataTableImporter _dataTableImporter;
        private readonly ILogger _logger;

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
        private readonly ConcurrentDictionary<int, ReleasedProvider> _releasedProvidersById = new ConcurrentDictionary<int, ReleasedProvider>();
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

        private readonly ConcurrentBag<FundingGroup> _createFundingGroups = new ConcurrentBag<FundingGroup>();
        private readonly ConcurrentBag<FundingGroupVersion> _createFundingGroupVersions = new ConcurrentBag<FundingGroupVersion>();
        private readonly ConcurrentBag<FundingGroupVersionVariationReason> _createFundingGroupVariationReasons = new ConcurrentBag<FundingGroupVersionVariationReason>();
        private readonly ConcurrentBag<ReleasedProvider> _createReleasedProviders = new ConcurrentBag<ReleasedProvider>();
        private readonly ConcurrentBag<ReleasedProviderVersion> _createReleasedProviderVersions = new ConcurrentBag<ReleasedProviderVersion>();
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
            IReleaseManagementMigrationCosmosProducerConsumer<PublishedFundingVersion> fundingMigrator,
            IReleaseManagementMigrationCosmosProducerConsumer<PublishedProviderVersion> providerMigrator,
            IBlobClient blobClient,
            IReleaseManagementDataTableImporter dataTableImporter,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(fundingMigrator, nameof(fundingMigrator));
            Guard.ArgumentNotNull(providerMigrator, nameof(providerMigrator));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(dataTableImporter, nameof(dataTableImporter));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _cosmosRepo = publishedFundingRepository;
            _fundingMigrator = fundingMigrator;
            _providerMigrator = providerMigrator;
            _blobClient = blobClient;
            _dataTableImporter = dataTableImporter;
            _logger = logger;
        }

        public async Task Migrate(Dictionary<string, FundingStream> fundingStreams,
            Dictionary<string, FundingPeriod> fundingPeriods,
            Dictionary<string, Channel> channels,
            Dictionary<string, SqlModels.GroupingReason> groupingReasons,
            Dictionary<string, SqlModels.VariationReason> variationReasons,
            Dictionary<string, SqlModels.Specification> specifications)
        {
            IDataTableImporter dataImporter = _dataTableImporter as IDataTableImporter;

            await _fundingMigrator.RunAsync(fundingStreams, fundingPeriods, channels,
                groupingReasons, variationReasons, specifications,
                _cosmosRepo.GetPublishedFundingIterator(CosmosBatchSize), ProcessPublishedFundingVersions);

            FundingGroupDataTableBuilder fundingGroupsBuilder = new FundingGroupDataTableBuilder();
            fundingGroupsBuilder.AddRows(_createFundingGroups.ToArray());
            _logger.Information($"Importing {_createFundingGroups.Count} FundingGroups");
            await dataImporter.ImportDataTable(fundingGroupsBuilder, SqlBulkCopyOptions.KeepIdentity);

            FundingGroupVersionDataTableBuilder fundingGroupVersionsBuilder = new FundingGroupVersionDataTableBuilder();
            fundingGroupVersionsBuilder.AddRows(_createFundingGroupVersions.ToArray());
            _logger.Information($"Importing {_createFundingGroupVersions.Count} FundingGroupVersions");
            await dataImporter.ImportDataTable(fundingGroupVersionsBuilder, SqlBulkCopyOptions.KeepIdentity);

            FundingGroupVersionVariationReasonsDataTableBuilder fundingVariationReasonsBuilder = new FundingGroupVersionVariationReasonsDataTableBuilder();
            fundingVariationReasonsBuilder.AddRows(_createFundingGroupVariationReasons.ToArray());
            _logger.Information($"Importing {_createFundingGroupVariationReasons.Count} FundingGroupVariationReasons");
            await dataImporter.ImportDataTable(fundingVariationReasonsBuilder, SqlBulkCopyOptions.KeepIdentity);

            await _providerMigrator.RunAsync(fundingStreams, fundingPeriods, channels,
                groupingReasons, variationReasons, specifications,
                _cosmosRepo.GetReleasedPublishedProviderIterator(CosmosBatchSize), ProcessPublishedProviderVersions);

            ReleasedProviderDataTableBuilder releasedProvidersBuilder = new ReleasedProviderDataTableBuilder();
            releasedProvidersBuilder.AddRows(_createReleasedProviders.ToArray());
            _logger.Information($"Importing {_createReleasedProviders.Count} ReleasedProviders");
            await dataImporter.ImportDataTable(releasedProvidersBuilder, SqlBulkCopyOptions.KeepIdentity);

            ReleasedProviderVersionDataTableBuilder releasedProviderVersionsBuilder = new ReleasedProviderVersionDataTableBuilder();
            releasedProviderVersionsBuilder.AddRows(_createReleasedProviderVersions.ToArray());
            _logger.Information($"Importing {_createReleasedProviderVersions.Count} ReleasedProviderVersions");
            await dataImporter.ImportDataTable(releasedProviderVersionsBuilder, SqlBulkCopyOptions.KeepIdentity);

            await PopulateLinkingTables(channels, variationReasons);

            await MigrateBlobs();
        }

        private static string GetFundingGroupDictionaryKey(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue)
        {
            return $"{channelId}_{specificationId}_{groupingReasonId}_{organisationGroupTypeClassification}_{organisationGroupIdentifierValue}";
        }

        protected async Task ProcessPublishedFundingVersions(CancellationToken cancellationToken,
            dynamic context,
            ArraySegment<PublishedFundingVersion> publishedFunding)
        {
            IReleaseManagementImportContext ctx = ((IReleaseManagementImportContext)context);
            List<Task> createFundingGroupsTasks = new List<Task>();

            foreach (PublishedFundingVersion fundingVersion in publishedFunding)
            {
                await Task.Run(() => GenerateFundingGroupsAndVersions(fundingVersion, ctx));
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

            string key = GetFundingGroupDictionaryKey(channelId, fundingVersion.SpecificationId, groupingReasonId, fundingVersion.OrganisationGroupTypeClassification, fundingVersion.OrganisationGroupIdentifierValue);

            _fundingGroups.TryGetValue(key, out var fundingGroup);

            if (fundingGroup == null)
            {
                fundingGroup = new FundingGroup()
                {
                    FundingGroupId = _nextFundingGroupId,
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

                Interlocked.Increment(ref _nextFundingGroupId);

                _createFundingGroups.Add(fundingGroup);
                _fundingGroups.AddOrUpdate(key, fundingGroup, (id, existing) => fundingGroup);
            }

            return fundingGroup;
        }

        private FundingGroupVersion GenerateFundingGroupVersion(Channel channel, FundingGroup fundingGroup, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            FundingGroupVersion fundingGroupVersion = new FundingGroupVersion()
            {
                FundingGroupVersionId = _nextFundingGroupVersionId,
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

            Interlocked.Increment(ref _nextFundingGroupVersionId);

            _createFundingGroupVersions.Add(fundingGroupVersion);

            _fundingGroupVersions.AddOrUpdate($"{channel.ChannelId}_{fundingGroupVersion.FundingId}", fundingGroupVersion, (id, existing) => { return fundingGroupVersion; });
            _publishedFundingVersions.AddOrUpdate($"{channel.ChannelId}_{fundingVersion.FundingId}", fundingVersion, (id, existing) => { return fundingVersion; });

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
                            FundingGroupVersionVariationReasonId = _nextFundingGroupVersionVariationReasonId,
                            FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                            VariationReasonId = value.VariationReasonId,
                        };

                        Interlocked.Increment(ref _nextFundingGroupVersionVariationReasonId);

                        _createFundingGroupVariationReasons.Add(reason);
                    }
                }
            }

            if (fundingGroupVersion.MajorVersion == 1 && !_createFundingGroupVariationReasons.Any(_ => _.VariationReasonId == ctx.VariationReasons["FundingUpdated"].VariationReasonId))
            {
                _createFundingGroupVariationReasons.Add(new FundingGroupVersionVariationReason()
                {
                    FundingGroupVersionVariationReasonId = _nextFundingGroupVersionVariationReasonId,
                    FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                    VariationReasonId = ctx.VariationReasons["FundingUpdated"].VariationReasonId,
                });

                Interlocked.Increment(ref _nextFundingGroupVersionVariationReasonId);
            }

            if (fundingGroupVersion.MajorVersion == 1 && !_createFundingGroupVariationReasons.Any(_ => _.VariationReasonId == ctx.VariationReasons["ProfilingUpdated"].VariationReasonId))
            {
                _createFundingGroupVariationReasons.Add(new FundingGroupVersionVariationReason()
                {
                    FundingGroupVersionVariationReasonId = _nextFundingGroupVersionVariationReasonId,
                    FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                    VariationReasonId = ctx.VariationReasons["ProfilingUpdated"].VariationReasonId,
                });

                Interlocked.Increment(ref _nextFundingGroupVersionVariationReasonId);
            }
        }

        protected async Task ProcessPublishedProviderVersions(CancellationToken cancellationToken,
            dynamic context,
            ArraySegment<PublishedProviderVersion> publishedProviders)
        {
            List<Task> createReleasedProvidersTasks = new List<Task>();

            foreach (PublishedProviderVersion providerVersion in publishedProviders)
            {
                _publishedProviderVersions.AddOrUpdate(providerVersion.ProviderId, providerVersion, (id, existing) => { return providerVersion; });

                await Task.Run(() => GenerateReleasedProvidersAndVersions(providerVersion));
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
            _releasedProviders.TryGetValue(releasedProviderKey, out var releasedProvider);

            if (releasedProvider != null) return releasedProvider;

            releasedProvider = new ReleasedProvider
            {
                ReleasedProviderId = _nextReleasedProviderId,
                SpecificationId = providerVersion.SpecificationId,
                ProviderId = providerVersion.ProviderId
            };

            Interlocked.Increment(ref _nextReleasedProviderId);

            _createReleasedProviders.Add(releasedProvider);
            _releasedProviders.AddOrUpdate(releasedProviderKey, releasedProvider, (id, existing) => releasedProvider);
            _releasedProvidersById.AddOrUpdate(releasedProvider.ReleasedProviderId, releasedProvider, (id, existing) => releasedProvider);

            return releasedProvider;
        }

        private ReleasedProviderVersion CreateReleasedProviderVersion(ReleasedProvider releasedProvider, PublishedProviderVersion providerVersion)
        {
            ReleasedProviderVersion releasedProviderVersion = new ReleasedProviderVersion
            {
                ReleasedProviderVersionId = _nextReleasedProviderVersionId,
                ReleasedProviderId = releasedProvider.ReleasedProviderId,
                MajorVersion = providerVersion.MajorVersion,
                MinorVersion = providerVersion.MinorVersion,
                FundingId = providerVersion.FundingId,
                TotalFunding = providerVersion.TotalFunding ?? 0m,
                CoreProviderVersionId = providerVersion.Provider.ProviderVersionId,
            };

            Interlocked.Increment(ref _nextReleasedProviderVersionId);

            _createReleasedProviderVersions.Add(releasedProviderVersion);
            _releasedProviderVersions.AddOrUpdate(releasedProviderVersion.FundingId, releasedProviderVersion, (id, existing) => releasedProviderVersion);

            return releasedProviderVersion;
        }

        private async Task PopulateLinkingTables(Dictionary<string, Channel> channels, Dictionary<string, SqlModels.VariationReason> variationReasons)
        {
            List<ReleasedProviderVersionChannel> createReleasedProviderVersionChannels = new List<ReleasedProviderVersionChannel>();
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
                        throw new KeyNotFoundException($"ReleasedProviderVersion not found for fundingId '{providerVersionFundingId}'. PublishedFundingVersion: {publishedFunding.Value.Id}");
                    }

                    ReleasedProvider releasedProvider = _releasedProvidersById[releasedProviderVersion.ReleasedProviderId];
                    PublishedProviderVersion publishedProviderVersion = _publishedProviderVersions[releasedProvider.ProviderId];

                    ReleasedProviderVersionChannel releasedProviderVersionChannel = new ReleasedProviderVersionChannel
                    {
                        ReleasedProviderVersionChannelId = _nextReleasedProviderVersionChannelId++,
                        ReleasedProviderVersionId = releasedProviderVersion.ReleasedProviderVersionId,
                        ChannelId = fundingGroupVersion.ChannelId,
                        StatusChangedDate = publishedProviderVersion.Date.UtcDateTime,
                        AuthorId = publishedProviderVersion.Author.Id,
                        AuthorName = publishedProviderVersion.Author.Name,
                    };

                    createReleasedProviderVersionChannels.Add(releasedProviderVersionChannel);

                    FundingGroupProvider fundingGroupProvider = new FundingGroupProvider
                    {
                        FundingGroupProviderId = _nextFundingGroupProviderId++,
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
                                    ReleasedProviderChannelVariationReasonId = _nextReleasedProviderChannelVariationReasonsId++,
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

            releasedChannelBuilder.AddRows(createReleasedProviderVersionChannels.ToArray());
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

            foreach (BlobToMigrate blob in _blobsToMigrate)
            {
                await throttle.WaitAsync();

                trackedTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await _blobClient.StartCopyFromUriAsync(blob.SourceContainer, blob.SourceFileName, blob.TargetContainer, blob.TargetFileName);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Failed to copy blob '{blob.SourceFileName}' to new {blob.TargetContainer}";

                        _logger.Error(ex, errorMessage);

                        throw new Exception(errorMessage, ex);
                    }
                    finally
                    {
                        throttle.Release();
                    }
                }));
            }

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
