using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class FundingGroupProviderPersistenceService : IFundingGroupProviderPersistenceService
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseToChannelSqlMappingContext _ctx;

        public FundingGroupProviderPersistenceService(IReleaseManagementRepository releaseManagementRepository,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));

            _repo = releaseManagementRepository;
            _ctx = releaseToChannelSqlMappingContext;
        }

        public async Task PersistFundingGroupProviders(int channelId, IEnumerable<(PublishedFundingVersion, OrganisationGroupResult)> fundingGroupData, IEnumerable<PublishedProviderVersion> providersInGroupsToRelease)
        {
            Dictionary<string, PublishedProviderVersion> providers = providersInGroupsToRelease.ToDictionary(_ => _.ProviderId);

            IEnumerable<string> batchProviderIds = providersInGroupsToRelease.Select(_ => _.ProviderId);

            foreach (var group in fundingGroupData)
            {
                if (!_ctx.FundingGroupVersions[channelId].TryGetValue(group.Item1.FundingId, out FundingGroupVersion fundingGroupVersion))
                {
                    throw new InvalidOperationException($"Unable to find FundingGroupVersion with funding ID '{group.Item1.FundingId}' in channel '{channelId}'");
                }

                var providersIdsInBatchForGroup = group.Item2.Providers.Select(_ => _.ProviderId).Union(batchProviderIds);
                if (!providersIdsInBatchForGroup.Any())
                {
                    throw new InvalidOperationException($"No providers for the current batch were found for group '{group.Item1.FundingId}'");
                }

                foreach (var providerId in providersIdsInBatchForGroup)
                {
                    string contextKey = $"{providerId}_{channelId}";

                    if (!_ctx.ReleasedProviderVersionChannels.TryGetValue(contextKey, out ReleasedProviderVersionChannel releasedProviderVersionChannel))
                    {
                        throw new InvalidOperationException($"Unable to find ReleasedProviderVersionChannel for context key '{contextKey}'");
                    }

                    FundingGroupProvider fundingGroupProvider = new FundingGroupProvider()
                    {
                        FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                        ReleasedProviderVersionChannelId = releasedProviderVersionChannel.ReleasedProviderVersionChannelId,
                    };

                    await _repo.CreateFundingGroupProviderUsingAmbientTransaction(fundingGroupProvider);
                }
            }
        }
    }
}
