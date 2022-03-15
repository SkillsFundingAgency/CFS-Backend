using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class FundingGroupProviderPersistenceService : IFundingGroupProviderPersistenceService
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseToChannelSqlMappingContext _ctx;
        private readonly IUniqueIdentifierProvider _fundingGroupIdentifierGenerator;

        public FundingGroupProviderPersistenceService(IReleaseManagementRepository releaseManagementRepository,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
            IUniqueIdentifierProvider fundingGroupIdentifierGenerator)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(fundingGroupIdentifierGenerator, nameof(fundingGroupIdentifierGenerator));

            _repo = releaseManagementRepository;
            _ctx = releaseToChannelSqlMappingContext;
            _fundingGroupIdentifierGenerator = fundingGroupIdentifierGenerator;
        }

        public async Task PersistFundingGroupProviders(int channelId, IEnumerable<GeneratedPublishedFunding> fundingGroupData, IEnumerable<PublishedProviderVersion> providersInGroupsToRelease)
        {
            Dictionary<string, PublishedProviderVersion> providers = providersInGroupsToRelease.ToDictionary(_ => _.ProviderId);

            IEnumerable<string> batchProviderIds = providersInGroupsToRelease.Select(_ => _.ProviderId);

            List<FundingGroupProvider> createFundingGroupProviders = new List<FundingGroupProvider>();

            foreach (GeneratedPublishedFunding group in fundingGroupData)
            {
                if (!_ctx.FundingGroupVersions[channelId].TryGetValue(group.PublishedFundingVersion.FundingId, out FundingGroupVersion fundingGroupVersion))
                {
                    throw new InvalidOperationException($"Unable to find FundingGroupVersion with funding ID '{group.PublishedFundingVersion.FundingId}' in channel '{channelId}'");
                }

                IEnumerable<string> providersIdsInBatchForGroup = group.OrganisationGroupResult.Providers.Select(_ => _.ProviderId).Union(batchProviderIds);
                if (!providersIdsInBatchForGroup.Any())
                {
                    throw new InvalidOperationException($"No providers for the current batch were found for group '{group.PublishedFundingVersion.FundingId}'");
                }

                foreach (string providerId in providersIdsInBatchForGroup)
                {
                    string contextKey = $"{providerId}_{channelId}";

                    if (!_ctx.ReleasedProviderVersionChannels.TryGetValue(contextKey, out Guid releasedProviderVersionChannelId))
                    {
                        throw new InvalidOperationException($"Unable to find ReleasedProviderVersionChannel for context key '{contextKey}'");
                    }

                    FundingGroupProvider fundingGroupProvider = new FundingGroupProvider()
                    {
                        FundingGroupProviderId = _fundingGroupIdentifierGenerator.GenerateIdentifier(),
                        FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                        ReleasedProviderVersionChannelId = releasedProviderVersionChannelId,
                    };

                    createFundingGroupProviders.Add(fundingGroupProvider);
                }
            }

            if (createFundingGroupProviders.Any())
            {
                await _repo.BulkCreateFundingGroupProvidersUsingAmbientTransaction(createFundingGroupProviders);
            }
        }
    }
}
