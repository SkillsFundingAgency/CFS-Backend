using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Storage.Blob;
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
        private readonly ICosmosProducerConsumer<PublishedFundingVersion> _fundingMigrator;
        private readonly ICosmosProducerConsumer<PublishedProviderVersion> _providerMigrator;
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _blobClientPolicy;

        private ConcurrentDictionary<string, PublishedFundingVersion> _publishedFundings = new ConcurrentDictionary<string, PublishedFundingVersion>();
        private ConcurrentDictionary<string, PublishedProviderVersion> _releasedPublishedProviders = new ConcurrentDictionary<string, PublishedProviderVersion>();

        public PublishedFundingReleaseManagementMigrator(IPublishedFundingRepository publishedFundingRepository,
            IReleaseManagementRepository releaseManagementRepository,
            ICosmosProducerConsumer<PublishedFundingVersion> fundingMigrator,
            ICosmosProducerConsumer<PublishedProviderVersion> providerMigrator,
            IBlobClient blobClient,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(fundingMigrator, nameof(fundingMigrator));
            Guard.ArgumentNotNull(providerMigrator, nameof(providerMigrator));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _cosmosRepo = publishedFundingRepository;
            _repo = releaseManagementRepository;
            _fundingMigrator = fundingMigrator;
            _providerMigrator = providerMigrator;
            _blobClient = blobClient;
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
            await _fundingMigrator.RunAsync(fundingStreams, fundingPeriods, channels,
                groupingReasons, variationReasons, specifications, releasedProviders, releasedProviderVersions,
                _cosmosRepo.GetPublishedFundingIterator(BatchSize), ProcessPublishedFundingVersions);

            await _providerMigrator.RunAsync(fundingStreams, fundingPeriods, channels,
                groupingReasons, variationReasons, specifications, releasedProviders, releasedProviderVersions,
                _cosmosRepo.GetReleasedPublishedProviderIterator(BatchSize), ProcessPublishedProviderVersions);
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

                _releasedPublishedProviders.AddOrUpdate(providerVersion.ProviderId, providerVersion, (id, existing) => { return providerVersion; });
            }

            await PopulateLinkingTables(ctx);
        }

        private async Task PopulateLinkingTables(IReleaseManagementImportContext ctx)
        {
            Dictionary<string, FundingGroupVersion> fundingGroupVersions =
                (await _repo.GetFundingGroupVersions()).ToDictionary(_ => _.FundingId, _ => _);

            Dictionary<int, ReleasedProvider> releasedProviders =
                (await _repo.GetReleasedProviders()).ToDictionary(_ => _.ReleasedProviderId, _ => _);

            IEnumerable<Channel> channels = await _repo.GetChannels();

            foreach (KeyValuePair<string, PublishedFundingVersion> publishedFunding in _publishedFundings)
            {
                foreach (string fundingId in publishedFunding.Value.ProviderFundings)
                {
                    FundingGroupVersion fundingGroupVersion = fundingGroupVersions[fundingId];

                    ReleasedProviderVersion releasedProviderVersion = ctx.ReleasedProviderVersion[fundingId];
                    ReleasedProvider releasedProvider = releasedProviders[releasedProviderVersion.ReleasedProviderId];
                    PublishedProviderVersion publishedProviderVersion = _releasedPublishedProviders[releasedProvider.ProviderId];

                    ReleasedProviderVersionChannel releasedProviderVersionChannel = new ReleasedProviderVersionChannel
                    {
                        ReleasedProviderVersionId = releasedProviderVersion.ReleasedProviderVersionId,
                        ChannelId = fundingGroupVersion.ChannelId,
                        StatusChangedDate = publishedProviderVersion.Date.UtcDateTime,
                        AuthorId = publishedProviderVersion.Author.Id,
                        AuthorName = publishedProviderVersion.Author.Name,
                    };

                    await _repo.CreateReleasedProviderVersionChannel(releasedProviderVersionChannel);

                    FundingGroupProvider fundingGroupProvider = new FundingGroupProvider
                    {
                        FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                        ReleasedProviderVersionChannelId = releasedProviderVersionChannel.ReleasedProviderVersionChannelId
                    };

                    await _repo.CreateFundingGroupProvider(fundingGroupProvider);

                    await MigrateBlob(publishedProviderVersion, channels.Single(_ => _.ChannelId == fundingGroupVersion.ChannelId).ChannelCode);

                    if (publishedProviderVersion.VariationReasons.AnyWithNullCheck())
                    {
                        foreach (CalculateFunding.Models.Publishing.VariationReason variationReason in publishedProviderVersion.VariationReasons)
                        {
                            if (ctx.VariationReasons.TryGetValue(variationReason.ToString(), out SqlModels.VariationReason value))
                            {
                                ReleasedProviderChannelVariationReason reason = new ReleasedProviderChannelVariationReason()
                                {
                                    ReleasedProviderVersionChannelId = releasedProviderVersionChannel.ReleasedProviderVersionChannelId,
                                    VariationReasonId = value.VariationReasonId,
                                };

                                await _repo.CreateReleasedProviderChannelVariationReason(reason);
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Variation reason '{variationReason}' does not exist. PublishedProviderVersion: {publishedProviderVersion.Id}");
                            }
                        }
                    }
                }
            }
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
                await MigrateBlob(fundingVersion, channel.ChannelCode);
            }
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

            await PopulateVariationReasonsForFundingGroupVersion(fundingGroupVersion, fundingVersion, ctx);

            return fundingGroupVersion;
        }

        private async Task PopulateVariationReasonsForFundingGroupVersion(FundingGroupVersion fundingGroupVersion, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            List<FundingGroupVersionVariationReason> createVariationReasons = new List<FundingGroupVersionVariationReason>();

            if (fundingVersion.VariationReasons.AnyWithNullCheck())
            {
                foreach (CalculateFunding.Models.Publishing.VariationReason variationReason in fundingVersion.VariationReasons)
                {
                    if (ctx.VariationReasons.TryGetValue(variationReason.ToString(), out SqlModels.VariationReason value))
                    {
                        FundingGroupVersionVariationReason reason = new FundingGroupVersionVariationReason()
                        {
                            FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                            VariationReasonId = value.VariationReasonId,
                        };

                        createVariationReasons.Add(reason);
                    }
                }
            }

            if (fundingGroupVersion.MajorVersion == 1 && !createVariationReasons.Any(_ => _.VariationReasonId == ctx.VariationReasons["FundingUpdated"].VariationReasonId))
            {
                createVariationReasons.Add(new FundingGroupVersionVariationReason()
                {
                    FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                    VariationReasonId = ctx.VariationReasons["FundingUpdated"].VariationReasonId,
                });
            }

            if (fundingGroupVersion.MajorVersion == 1 && !createVariationReasons.Any(_ => _.VariationReasonId == ctx.VariationReasons["ProfilingUpdated"].VariationReasonId))
            {
                createVariationReasons.Add(new FundingGroupVersionVariationReason()
                {
                    FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                    VariationReasonId = ctx.VariationReasons["ProfilingUpdated"].VariationReasonId,
                });
            }

            foreach (var reason in createVariationReasons)
            {
                await _repo.CreateFundingGroupVariationReason(reason);

            }
        }

        private async Task<FundingGroup> GetOrGenerateFunding(int channelId, PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            int groupingReasonId = ctx.GroupingReasons[fundingVersion.GroupingReason.ToString()].GroupingReasonId;

            FundingGroup fundingGroup = await _repo.GetFundingGroup(channelId, fundingVersion.SpecificationId, groupingReasonId, fundingVersion.OrganisationGroupTypeClassification, fundingVersion.OrganisationGroupIdentifierValue);

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
