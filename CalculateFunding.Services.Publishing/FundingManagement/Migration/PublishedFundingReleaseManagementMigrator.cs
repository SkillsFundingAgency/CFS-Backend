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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class PublishedFundingReleaseManagementMigrator : IPublishedFundingReleaseManagementMigrator
    {
        private const int BatchSize = 50;

        private readonly IPublishedFundingRepository _cosmosRepo;
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseManagementMigrationCosmosProducerConsumer<PublishedFundingVersion> _fundingMigrator;
        private readonly IReleaseManagementMigrationCosmosProducerConsumer<PublishedProviderVersion> _providerMigrator;
        private readonly IBlobClient _blobClient;
        private readonly IReleaseManagementDataTableImporter _dataTableImporter;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _blobClientPolicy;

        private ConcurrentDictionary<string, PublishedFundingVersion> _publishedFundings = new ConcurrentDictionary<string, PublishedFundingVersion>();
        private ConcurrentDictionary<string, PublishedProviderVersion> _publishedProviderVersions = new ConcurrentDictionary<string, PublishedProviderVersion>();
        private ConcurrentDictionary<string, FundingGroupVersion> _fundingGroupVersions = new ConcurrentDictionary<string, FundingGroupVersion>();
        private ConcurrentDictionary<int, ReleasedProvider> _releasedProviders = new ConcurrentDictionary<int, ReleasedProvider>();
        private ConcurrentDictionary<string, FundingGroup> _fundingGroups = new ConcurrentDictionary<string, FundingGroup>();
        private ConcurrentBag<FundingGroupVersionVariationReason> _createFundingGroupVariationReasons = new ConcurrentBag<FundingGroupVersionVariationReason>();
        private ConcurrentBag<ReleasedProviderChannelVariationReason> _createReleasedProviderChannelVariationReasons = new ConcurrentBag<ReleasedProviderChannelVariationReason>();
        private ConcurrentBag<Task> _blobMigrationTasks = new ConcurrentBag<Task>();
        private Dictionary<string, int> _lastIds;

        public PublishedFundingReleaseManagementMigrator(IPublishedFundingRepository publishedFundingRepository,
            IReleaseManagementRepository releaseManagementRepository,
            IReleaseManagementMigrationCosmosProducerConsumer<PublishedFundingVersion> fundingMigrator,
            IReleaseManagementMigrationCosmosProducerConsumer<PublishedProviderVersion> providerMigrator,
            IBlobClient blobClient,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IReleaseManagementDataTableImporter dataTableImporter,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(fundingMigrator, nameof(fundingMigrator));
            Guard.ArgumentNotNull(providerMigrator, nameof(providerMigrator));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(dataTableImporter, nameof(dataTableImporter));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _cosmosRepo = publishedFundingRepository;
            _repo = releaseManagementRepository;
            _fundingMigrator = fundingMigrator;
            _providerMigrator = providerMigrator;
            _blobClient = blobClient;
            _dataTableImporter = dataTableImporter;
            _blobClientPolicy = publishingResiliencePolicies.BlobClient;
            _logger = logger;
        }

        public async Task Migrate(Dictionary<string, FundingStream> fundingStreams,
            Dictionary<string, FundingPeriod> fundingPeriods,
            Dictionary<string, Channel> channels,
            Dictionary<string, SqlModels.GroupingReason> groupingReasons,
            Dictionary<string, SqlModels.VariationReason> variationReasons,
            Dictionary<string, SqlModels.Specification> specifications,
            Dictionary<string, ReleasedProvider> releasedProviders,
            Dictionary<string, ReleasedProviderVersion> releasedProviderVersions)
        {
            _lastIds = (await _repo.GetLastIdSummary()).ToDictionary(_ => _.TableName, _ => _.LastId.HasValue ? _.LastId.Value : 0);

            await PopulateFundingGroups();

            await _fundingMigrator.RunAsync(fundingStreams, fundingPeriods, channels,
                groupingReasons, variationReasons, specifications, releasedProviders, releasedProviderVersions,
                _cosmosRepo.GetPublishedFundingIterator(BatchSize), ProcessPublishedFundingVersions);

            await _providerMigrator.RunAsync(fundingStreams, fundingPeriods, channels,
                groupingReasons, variationReasons, specifications, releasedProviders, releasedProviderVersions,
                _cosmosRepo.GetReleasedPublishedProviderIterator(BatchSize), ProcessPublishedProviderVersions);

            await PopulateLinkingTables();

            FundingGroupVersionVariationReasonsDataTableBuilder fgvvrBuilder = new FundingGroupVersionVariationReasonsDataTableBuilder();
            fgvvrBuilder.AddRows(_createFundingGroupVariationReasons.ToArray());
            await (_dataTableImporter as IDataTableImporter).ImportDataTable(fgvvrBuilder);

            ReleasedProviderChannelVariationReasonDataTableBuilder rpcvrBuilder = new ReleasedProviderChannelVariationReasonDataTableBuilder();
            rpcvrBuilder.AddRows(_createReleasedProviderChannelVariationReasons.ToArray());
            await (_dataTableImporter as IDataTableImporter).ImportDataTable(rpcvrBuilder);

            await MigrateBlobs(_blobMigrationTasks);
        }

        private async Task PopulateFundingGroups()
        {
            IEnumerable<FundingGroup> fundingGroups = await _repo.GetFundingGroups();
            foreach (FundingGroup fundingGroup in fundingGroups)
            {
                string key = GetFundingGroupDictionaryKey(fundingGroup.ChannelId, fundingGroup.SpecificationId, fundingGroup.GroupingReasonId, fundingGroup.OrganisationGroupTypeClassification, fundingGroup.OrganisationGroupIdentifierValue);
                _fundingGroups.AddOrUpdate(key, fundingGroup, (id, existing) => { return fundingGroup; });
            }
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

            foreach (PublishedFundingVersion fundingVersion in publishedFunding)
            {
                _logger.Information($"Migrating published funding version {fundingVersion.Id}");

                await GenerateFundingGroupsAndVersions(fundingVersion, ctx);

                _publishedFundings.AddOrUpdate(fundingVersion.FundingId, fundingVersion, (id, existing) => { return fundingVersion; });
            }
        }

        protected async Task ProcessPublishedProviderVersions(CancellationToken cancellationToken,
            dynamic context,
            ArraySegment<PublishedProviderVersion> publishedProviders)
        {
            IReleaseManagementImportContext ctx = ((IReleaseManagementImportContext)context);

            foreach (PublishedProviderVersion providerVersion in publishedProviders)
            {
                _logger.Information($"Migrating provider version {providerVersion.Id}");

                await GenerateReleasedProvidersAndVersions(providerVersion, ctx);

                _publishedProviderVersions.AddOrUpdate(providerVersion.ProviderId, providerVersion, (id, existing) => { return providerVersion; });
            }
        }

        private async Task PopulateLinkingTables()
        {
            IEnumerable<Channel> channels = await _repo.GetChannels();

            Dictionary<string, ReleasedProviderVersion> releasedProviderVersions =
                (await _repo.GetReleasedProviderVersions()).ToDictionary(_ => _.FundingId, _ => _);
            Dictionary<string, SqlModels.VariationReason> variationReasons = (await _repo.GetVariationReasons()).ToDictionary(_ => _.VariationReasonCode, _ => _);

            List<ReleasedProviderVersionChannel> newReleasedProviderVersionChannels = new List<ReleasedProviderVersionChannel>();
            List<FundingGroupProvider> newFundingGroupProviders = new List<FundingGroupProvider>();

            ReleasedProviderVersionChannelDataTableBuilder rpvcBuilder = new ReleasedProviderVersionChannelDataTableBuilder();
            FundingGroupProviderDataTableBuilder fgpBuilder = new FundingGroupProviderDataTableBuilder();

            int nextReleasedProviderVersionChannelId = _lastIds["ReleasedProviderVersionChannels"] + 1;
            int nextFundingGroupProviderId = _lastIds["FundingGroupProviders"] + 1; ;
            int nextReleasedProviderChannelVariationReasonsId = _lastIds["ReleasedProviderChannelVariationReasons"] + 1;

            List<Task> blobMigrationTasks = new List<Task>();

            foreach (KeyValuePair<string, PublishedFundingVersion> publishedFunding in _publishedFundings)
            {
                foreach (string fundingId in publishedFunding.Value.ProviderFundings)
                {
                    FundingGroupVersion fundingGroupVersion = _fundingGroupVersions[fundingId];

                    ReleasedProviderVersion releasedProviderVersion = releasedProviderVersions[fundingId];
                    ReleasedProvider releasedProvider = _releasedProviders[releasedProviderVersion.ReleasedProviderId];
                    PublishedProviderVersion publishedProviderVersion = _publishedProviderVersions[releasedProvider.ProviderId];

                    ReleasedProviderVersionChannel releasedProviderVersionChannel = new ReleasedProviderVersionChannel
                    {
                        ReleasedProviderVersionChannelId = nextReleasedProviderVersionChannelId++,
                        ReleasedProviderVersionId = releasedProviderVersion.ReleasedProviderVersionId,
                        ChannelId = fundingGroupVersion.ChannelId,
                        StatusChangedDate = publishedProviderVersion.Date.UtcDateTime,
                        AuthorId = publishedProviderVersion.Author.Id,
                        AuthorName = publishedProviderVersion.Author.Name,
                    };

                    newReleasedProviderVersionChannels.Add(releasedProviderVersionChannel);

                    FundingGroupProvider fundingGroupProvider = new FundingGroupProvider
                    {
                        FundingGroupProviderId = nextFundingGroupProviderId++,
                        FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                        ReleasedProviderVersionChannelId = releasedProviderVersionChannel.ReleasedProviderVersionChannelId
                    };

                    newFundingGroupProviders.Add(fundingGroupProvider);

                    if (publishedProviderVersion.VariationReasons.AnyWithNullCheck())
                    {
                        foreach (CalculateFunding.Models.Publishing.VariationReason variationReason in publishedProviderVersion.VariationReasons)
                        {
                            if (variationReasons.TryGetValue(variationReason.ToString(), out SqlModels.VariationReason value))
                            {
                                ReleasedProviderChannelVariationReason reason = new ReleasedProviderChannelVariationReason()
                                {
                                    ReleasedProviderChannelVariationReasonId = nextReleasedProviderChannelVariationReasonsId++,
                                    ReleasedProviderVersionChannelId = releasedProviderVersionChannel.ReleasedProviderVersionChannelId,
                                    VariationReasonId = value.VariationReasonId,
                                };

                                _createReleasedProviderChannelVariationReasons.Add(reason);
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Variation reason '{variationReason}' does not exist. PublishedProviderVersion: {publishedProviderVersion.Id}");
                            }
                        }
                    }

                    _blobMigrationTasks.Add(MigrateBlob(publishedProviderVersion, channels.Single(_ => _.ChannelId == fundingGroupVersion.ChannelId).ChannelCode));
                }
            }

            rpvcBuilder.AddRows(newReleasedProviderVersionChannels.ToArray());
            fgpBuilder.AddRows(newFundingGroupProviders.ToArray());

            IDataTableImporter importer = _dataTableImporter as IDataTableImporter;
            await importer.ImportDataTable(rpvcBuilder);
            await importer.ImportDataTable(fgpBuilder);
        }

        private async Task GenerateReleasedProvidersAndVersions(PublishedProviderVersion providerVersion, IReleaseManagementImportContext ctx)
        {
            ReleasedProvider releasedProvider;
            string releasedProviderKey = $"{providerVersion.ProviderId}_{providerVersion.SpecificationId}";

            if (!ctx.ReleasedProviders.ContainsKey(releasedProviderKey))
            {
                releasedProvider = await CreateReleasedProvider(providerVersion);
                ctx.ReleasedProviders.Add(releasedProviderKey, releasedProvider);
            }
            else
            {
                releasedProvider = ctx.ReleasedProviders[releasedProviderKey];
            }

            _releasedProviders.AddOrUpdate(releasedProvider.ReleasedProviderId, releasedProvider, (id, existing) => { return releasedProvider; });

            if (!ctx.ReleasedProviderVersion.ContainsKey(providerVersion.FundingId))
            {
                ReleasedProviderVersion releasedProviderVersion = await CreateReleasedProviderVersion(releasedProvider, providerVersion);
                ctx.ReleasedProviderVersion.Add(releasedProviderVersion.FundingId, releasedProviderVersion);
            }
        }

        private async Task<ReleasedProvider> CreateReleasedProvider(PublishedProviderVersion providerVersion)
        {
            ReleasedProvider releasedProvider = new ReleasedProvider
            {
                SpecificationId = providerVersion.SpecificationId,
                ProviderId = providerVersion.ProviderId
            };

            await _repo.CreateReleasedProvider(releasedProvider);

            return releasedProvider;
        }

        private async Task<ReleasedProviderVersion> CreateReleasedProviderVersion(ReleasedProvider releasedProvider, PublishedProviderVersion providerVersion)
        {
            ReleasedProviderVersion releasedProviderVersion = new ReleasedProviderVersion
            {
                ReleasedProviderId = releasedProvider.ReleasedProviderId,
                MajorVersion = providerVersion.MajorVersion,
                MinorVersion = providerVersion.MinorVersion,
                FundingId = providerVersion.FundingId,
                TotalFunding = providerVersion.TotalFunding ?? 0m
            };

            await _repo.CreateReleasedProviderVersion(releasedProviderVersion);

            return releasedProviderVersion;
        }

        private async Task GenerateFundingGroupsAndVersions(PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            // Generate eligible channels
            IEnumerable<Channel> channels = GetChannelsForExistingFundingVersion(fundingVersion.GroupingReason, ctx.Channels);

            foreach (var channel in channels)
            {
                FundingGroup fundingGroup = await GetOrGenerateFunding(channel.ChannelId, fundingVersion, ctx);
                FundingGroupVersion fundingGroupVersion = await GetOrGenerateFundingGroupVersion(channel, fundingGroup, fundingVersion, ctx);
                _fundingGroupVersions.AddOrUpdate(fundingGroupVersion.FundingId, fundingGroupVersion, (id, existing) => { return fundingGroupVersion; });

                _blobMigrationTasks.Add(MigrateBlob(fundingVersion, channel.ChannelCode));
            }
        }

        private async Task MigrateBlobs(IEnumerable<Task> blobMigrationTasks)
        {
            SemaphoreSlim throttle = new SemaphoreSlim(50);
            List<Task> trackedTasks = new List<Task>();

            foreach (Task blobMigrationTask in blobMigrationTasks)
            {
                try
                {
                    await throttle.WaitAsync();
                    trackedTasks.Add(Task.Run(async () =>
                    {
                        await blobMigrationTask;
                    }));
                }
                finally
                {
                    throttle.Release();
                }
            }

            await TaskHelper.WhenAllAndThrow(trackedTasks.ToArray());
        }

        private async Task MigrateBlob(PublishedFundingVersion publishedFundingVersion, string channelCode)
        {
            string blobName = $"{publishedFundingVersion.FundingStreamId}-{publishedFundingVersion.FundingPeriod.Id}-{publishedFundingVersion.GroupingReason}-{publishedFundingVersion.OrganisationGroupTypeCode}-{publishedFundingVersion.OrganisationGroupIdentifierValue}-{publishedFundingVersion.MajorVersion}_{publishedFundingVersion.MinorVersion}.json";

            try
            {
                await _blobClient.StartCopyFromUriAsync("publishedfunding", blobName, "releasedgroups", $"{channelCode}/{blobName}");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to copy blob '{blobName}' to new container";

                _logger.Error(ex, errorMessage);

                throw new Exception(errorMessage, ex);
            }
        }

        private async Task MigrateBlob(PublishedProviderVersion publishedProviderVersion, string channelCode)
        {
            string blobName = $"{publishedProviderVersion.FundingStreamId}-{publishedProviderVersion.FundingPeriodId}-{publishedProviderVersion.ProviderId}-{publishedProviderVersion.MajorVersion}-{publishedProviderVersion.MinorVersion}.json";

            try
            {
                await _blobClient.StartCopyFromUriAsync("publishedproviderversions", blobName, "releasedproviders", $"{channelCode}/{blobName}");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to copy blob '{blobName}' to new container";

                _logger.Error(ex, errorMessage);

                throw new Exception(errorMessage, ex);
            }
        }

        private async Task<FundingGroupVersion> GetOrGenerateFundingGroupVersion(Channel channel, FundingGroup fundingGroup, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            FundingGroupVersion existingVersion = await _repo.GetFundingGroupVersion(fundingGroup.FundingGroupId, fundingVersion.MajorVersion);
            if (existingVersion != null)
            {
                return existingVersion;
            }

            FundingGroupVersion fundingGroupVersion = new FundingGroupVersion()
            {
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

            fundingGroupVersion = await _repo.CreateFundingGroupVersion(fundingGroupVersion);

            PopulateVariationReasonsForFundingGroupVersion(fundingGroupVersion, fundingVersion, ctx);

            return fundingGroupVersion;
        }

        private void PopulateVariationReasonsForFundingGroupVersion(FundingGroupVersion fundingGroupVersion, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            int nextFundingGroupVersionVariationReasonId = _lastIds["FundingGroupVersionVariationReasons"] + 1;

            if (fundingVersion.VariationReasons.AnyWithNullCheck())
            {
                foreach (CalculateFunding.Models.Publishing.VariationReason variationReason in fundingVersion.VariationReasons)
                {
                    if (ctx.VariationReasons.TryGetValue(variationReason.ToString(), out SqlModels.VariationReason value))
                    {
                        FundingGroupVersionVariationReason reason = new FundingGroupVersionVariationReason()
                        {
                            FundingGroupVersionVariationReasonId = nextFundingGroupVersionVariationReasonId++,
                            FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                            VariationReasonId = value.VariationReasonId,
                        };

                        _createFundingGroupVariationReasons.Add(reason);
                    }
                }
            }

            if (fundingGroupVersion.MajorVersion == 1 && !_createFundingGroupVariationReasons.Any(_ => _.VariationReasonId == ctx.VariationReasons["FundingUpdated"].VariationReasonId))
            {
                _createFundingGroupVariationReasons.Add(new FundingGroupVersionVariationReason()
                {
                    FundingGroupVersionVariationReasonId = nextFundingGroupVersionVariationReasonId++,
                    FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                    VariationReasonId = ctx.VariationReasons["FundingUpdated"].VariationReasonId,
                });
            }

            if (fundingGroupVersion.MajorVersion == 1 && !_createFundingGroupVariationReasons.Any(_ => _.VariationReasonId == ctx.VariationReasons["ProfilingUpdated"].VariationReasonId))
            {
                _createFundingGroupVariationReasons.Add(new FundingGroupVersionVariationReason()
                {
                    FundingGroupVersionVariationReasonId = nextFundingGroupVersionVariationReasonId++,
                    FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                    VariationReasonId = ctx.VariationReasons["ProfilingUpdated"].VariationReasonId,
                });
            }
        }

        private async Task<FundingGroup> GetOrGenerateFunding(int channelId, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            int groupingReasonId = ctx.GroupingReasons[fundingVersion.GroupingReason.ToString()].GroupingReasonId;

            string key = GetFundingGroupDictionaryKey(channelId, fundingVersion.SpecificationId, groupingReasonId, fundingVersion.OrganisationGroupTypeClassification, fundingVersion.OrganisationGroupIdentifierValue);

            FundingGroup fundingGroup;
            _fundingGroups.TryGetValue(key, out fundingGroup);

            if (fundingGroup == null)
            {
                fundingGroup = new FundingGroup()
                {
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

                fundingGroup = await _repo.CreateFundingGroup(fundingGroup);
                _fundingGroups.AddOrUpdate(key, fundingGroup, (id, existing) => { return fundingGroup; });
            }

            return fundingGroup;
        }

        private IEnumerable<Channel> GetChannelsForExistingFundingVersion(
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
    }
}
