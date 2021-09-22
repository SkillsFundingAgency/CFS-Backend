using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class PublishedFundingReleaseManagementMigrator : IPublishedFundingReleaseManagementMigrator
    {
        private readonly IPublishedFundingRepository _cosmosRepo;
        private readonly IReleaseManagementRepository _repo;
        private readonly IPoliciesApiClient _policiesClient;
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly ILogger _logger;

        private Dictionary<string, FundingConfiguration> _fundingConfigs = new Dictionary<string, FundingConfiguration>();

        public PublishedFundingReleaseManagementMigrator(IPublishedFundingRepository publishedFundingRepository,
            IReleaseManagementRepository releaseManagementRepository,
            IProducerConsumerFactory producerConsumerFactory,
            IPoliciesApiClient policiesApiClient,
            ILogger logger)
        {
            _cosmosRepo = publishedFundingRepository;
            _repo = releaseManagementRepository;
            _policiesClient = policiesApiClient;
            _producerConsumerFactory = producerConsumerFactory;
            _logger = logger;
        }
        public async Task Migrate(Dictionary<string, FundingStream> fundingStreams,
            Dictionary<string, FundingPeriod> fundingPeriods,
            Dictionary<string, Channel> channels,
            Dictionary<string, SqlModels.GroupingReason> groupingReasons,
            Dictionary<string, SqlModels.VariationReason> variationReasons,
            Dictionary<string, SqlModels.Specification> specifications)
        {
            IReleaseManagementImportContext importContext = new ReleaseManagementImportContext()
            {
                Documents = _cosmosRepo.GetPublishedFundingIterator(50),
                FundingStreams = fundingStreams,
                FundingPeriods = fundingPeriods,
                Channels = channels,
                GroupingReasons = groupingReasons,
                VariationReasons = variationReasons,
                Specifications = specifications,
                JobId = Guid.NewGuid().ToString(),
            };


            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProducePublishedFunding,
               PopulateDataTables,
               10,
               1,
               _logger);

            await producerConsumer.Run(importContext);
        }

        private async Task<(bool isComplete, IEnumerable<PublishedFundingVersion> items)> ProducePublishedFunding(CancellationToken cancellationToken,
           dynamic context)
        {
            try
            {
                ICosmosDbFeedIterator feed = ((IReleaseManagementImportContext)context).Documents;

                if (!feed.HasMoreResults)
                {
                    return (true, ArraySegment<PublishedFundingVersion>.Empty);
                }

                IEnumerable<PublishedFundingVersion> documents = await feed.ReadNext<PublishedFundingVersion>(cancellationToken);

                while (documents.IsNullOrEmpty() && feed.HasMoreResults)
                {
                    documents = await feed.ReadNext<PublishedFundingVersion>(cancellationToken);
                }

                if (documents.IsNullOrEmpty() && !feed.HasMoreResults)
                {
                    return (true, ArraySegment<PublishedFundingVersion>.Empty);
                }

                return (false, documents.ToArray());
            }
            catch
            {
                return (true, ArraySegment<PublishedFundingVersion>.Empty);
            }
        }

        protected async Task PopulateDataTables(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<PublishedFundingVersion> publishedProviders)
        {
            IReleaseManagementImportContext ctx = ((IReleaseManagementImportContext)context);

            foreach (PublishedFundingVersion fundingVersion in publishedProviders)
            {
                Console.WriteLine($"Processing {fundingVersion.Id}");

                await ProcessFundingVersion(fundingVersion, ctx);

            }
        }

        private async Task ProcessFundingVersion(PublishedFundingVersion fundingVersion, IReleaseManagementImportContext ctx)
        {
            FundingConfiguration fundingConfiguration = await GetFundingConfiguration(fundingVersion.FundingStreamId, fundingVersion.FundingPeriod.Id);

            // Generate eligible channels
            IEnumerable<Channel> channels = GetChannelsForExistingFundingVersion(fundingVersion.GroupingReason, ctx.Channels);

            foreach (var channel in channels)
            {
                FundingGroup fundingGroup = await GetOrGenerateFunding(channel.ChannelId, fundingVersion, ctx);

                FundingGroupVersion fundingGroupVersion = await GetOrGenerateFundingGroupVersion(channel, fundingGroup, fundingVersion, ctx);

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
                foreach (var variationReason in fundingVersion.VariationReasons)
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

        private async Task<FundingConfiguration> GetFundingConfiguration(string fundingStreamId, string fundingPeriodId)
        {
            string key = $"{fundingStreamId}-{fundingPeriodId}";

            if (_fundingConfigs.TryGetValue(key, out FundingConfiguration fundingConfiguration))
            {
                return fundingConfiguration;
            }

            var fundingConfigurationResult = await _policiesClient.GetFundingConfiguration(fundingStreamId, fundingPeriodId);

            if (fundingConfigurationResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Unable to get funding config for {fundingStreamId} {fundingPeriodId}");
            }

            fundingConfiguration = fundingConfigurationResult.Content;

            _fundingConfigs.Add(key, fundingConfiguration);

            return fundingConfiguration;
        }
    }
}
