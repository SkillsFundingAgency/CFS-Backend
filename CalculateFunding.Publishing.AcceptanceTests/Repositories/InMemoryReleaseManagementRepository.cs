using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryReleaseManagementRepository : IReleaseManagementRepository
    {
        readonly ConcurrentDictionary<string, FundingPeriod> _fundingPeriods = new();
        readonly ConcurrentDictionary<string, FundingStream> _fundingStreams = new();
        readonly ConcurrentDictionary<string, Specification> _specifications = new();
        readonly ConcurrentDictionary<string, GroupingReason> _groupingReasons = new();
        readonly ConcurrentDictionary<int, VariationReason> _variationReasons = new();

        readonly ConcurrentDictionary<string, Channel> _channels = new();

        readonly ConcurrentDictionary<int, FundingGroup> _fundingGroups = new();
        readonly ConcurrentDictionary<int, FundingGroupVersion> _fundingGroupVersions = new();
        readonly ConcurrentDictionary<int, FundingGroupVersionVariationReason> _fundingGroupVersionsVariationReasons = new();

        readonly ConcurrentDictionary<int, FundingGroupProvider> _fundingGroupProviders = new();

        readonly ConcurrentDictionary<int, ReleasedProvider> _releasedProviders = new();
        readonly ConcurrentDictionary<int, ReleasedProviderVersion> _releasedProviderVersions = new();
        readonly ConcurrentDictionary<int, ReleasedProviderVersionChannel> _releasedProviderVersionChannels = new();

        int _fundingGroupNextId = 1;
        int _fundingGroupVersionNextId = 1;
        int _fundingGroupVersionVariationReasonsNextId = 1;
        int _fundingGroupProvidersNextId = 1;
        int _releasedProvidersNextId = 1;
        int _releasedProviderVersionsNextId = 1;
        int _releasedProviderVersionChannelsNextId = 1;
        int _variationReasonsNextId = 1;

        public Task ClearDatabase()
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsFundingId(int? channelId, string fundingId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsProviderVersion(int channelId, string providerFundingVersion)
        {
            throw new NotImplementedException();
        }

        public Task<Channel> CreateChannel(Channel channel)
        {
            Guard.ArgumentNotNull(channel, nameof(channel));

            if (!_channels.TryAdd(channel.ChannelCode, channel))
            {
                throw new InvalidOperationException("Channel already exists");
            }

            return Task.FromResult(channel);
        }

        public Task<FundingGroup> CreateFundingGroup(FundingGroup fundingGroup)
        {
            if (fundingGroup.FundingGroupId <= 0)
            {
                fundingGroup.FundingGroupId = _fundingGroupNextId++;
            }

            if (!_fundingGroups.TryAdd(fundingGroup.FundingGroupId, fundingGroup))
            {
                throw new InvalidOperationException($"Funding group with id '{fundingGroup.FundingGroupId}' already exists");
            }

            return Task.FromResult(fundingGroup);

        }

        public Task<FundingGroupProvider> CreateFundingGroupProvider(FundingGroupProvider fundingGroupProvider)
        {
            throw new NotImplementedException();
        }

        public Task<FundingGroupProvider> CreateFundingGroupProviderUsingAmbientTransaction(FundingGroupProvider fundingGroupProvider)
        {
            if (fundingGroupProvider.FundingGroupProviderId <= 0)
            {
                fundingGroupProvider.FundingGroupProviderId = _fundingGroupProvidersNextId++;
            }

            EnsureFundingGroupVersionExists(fundingGroupProvider.FundingGroupVersionId);
            EnsureReleaseProviderVersionChannelExists(fundingGroupProvider.ReleasedProviderVersionChannelId);

            if (!_fundingGroupProviders.TryAdd(fundingGroupProvider.FundingGroupProviderId, fundingGroupProvider))
            {
                throw new InvalidOperationException($"Funding group provider already exists with ID '{fundingGroupProvider.FundingGroupProviderId}'");
            }

            return Task.FromResult(fundingGroupProvider);
        }

        private void EnsureReleaseProviderVersionChannelExists(int releasedProviderVersionChannelId)
        {
            if (!_releasedProviderVersionChannels.ContainsKey(releasedProviderVersionChannelId))
            {
                throw new InvalidOperationException($"Released provider version channel with ID '{releasedProviderVersionChannelId}' does not exist");
            }
        }

        public Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReason(FundingGroupVersionVariationReason reason)
        {
            throw new NotImplementedException();
        }

        public Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReasonUsingAmbientTransaction(FundingGroupVersionVariationReason reason)
        {
            if (reason.FundingGroupVersionVariationReasonId <= 0)
            {
                reason.FundingGroupVersionVariationReasonId = _fundingGroupVersionVariationReasonsNextId++;
            }

            EnsureFundingGroupVersionExists(reason.FundingGroupVersionId);
            EnsureVariationReasonExists(reason.VariationReasonId);

            if (!_fundingGroupVersionsVariationReasons.TryAdd(reason.FundingGroupVersionVariationReasonId, reason))
            {
                throw new InvalidOperationException($"Funding group version variation reason with id '{reason.FundingGroupVersionVariationReasonId}' already exists");
            }

            return Task.FromResult(reason);
        }

        private void EnsureVariationReasonExists(int variationReasonId)
        {
            if (!_variationReasons.ContainsKey(variationReasonId))
            {
                throw new InvalidOperationException($"Variation reason ID '{variationReasonId}' not found");
            }
        }

        private void EnsureFundingGroupVersionExists(int fundingGroupVersionId)
        {
            if (!_fundingGroupVersions.ContainsKey(fundingGroupVersionId))
            {
                throw new InvalidOperationException($"Funding group version ID '{fundingGroupVersionId}' not found");
            }
        }

        public Task<FundingGroupVersion> CreateFundingGroupVersion(FundingGroupVersion fundingGroupVersion)
        {
            throw new NotImplementedException();
        }

        public Task<FundingGroupVersion> CreateFundingGroupVersionUsingAmbientTransaction(FundingGroupVersion fundingGroupVersion)
        {
            if (fundingGroupVersion.FundingGroupVersionId <= 0)
            {
                fundingGroupVersion.FundingGroupVersionId = _fundingGroupVersionNextId++;
            }

            EnsureFundingGroupExists(fundingGroupVersion.FundingGroupId);

            if (!_fundingGroupVersions.TryAdd(fundingGroupVersion.FundingGroupVersionId, fundingGroupVersion))
            {
                throw new InvalidOperationException($"Funding group version with ID '{fundingGroupVersion.FundingGroupVersionId}' already exists");
            }

            return Task.FromResult(fundingGroupVersion);
        }

        public ReleasedProvider GetReleasedProviderById(int releasedProviderId)
        {
            return _releasedProviders[releasedProviderId];
        }

        private void EnsureFundingGroupExists(int fundingGroupId)
        {
            if (!_fundingGroups.ContainsKey(fundingGroupId))
            {
                throw new InvalidOperationException($"Funding group ID '{fundingGroupId}' not found");
            }
        }

        public Task<FundingPeriod> CreateFundingPeriod(FundingPeriod fundingPeriod)
        {
            _fundingPeriods.TryAdd(fundingPeriod.FundingPeriodCode, fundingPeriod);
            return Task.FromResult(fundingPeriod);
        }

        public Task<FundingPeriod> CreateFundingPeriodUsingAmbientTransaction(FundingPeriod fundingPeriod)
        {
            throw new NotImplementedException();
        }

        public Task<FundingStream> CreateFundingStream(FundingStream fundingStream)
        {
            _fundingStreams.TryAdd(fundingStream.FundingStreamCode, fundingStream);
            return Task.FromResult(fundingStream);
        }

        public Task<FundingStream> CreateFundingStreamUsingAmbientTransaction(FundingStream fundingStream)
        {
            throw new NotImplementedException();
        }

        public Task<GroupingReason> CreateGroupingReason(GroupingReason groupingReason)
        {
            if (!_groupingReasons.TryAdd(groupingReason.GroupingReasonCode, groupingReason))
            {
                throw new InvalidOperationException($"Grouping reason with ID '{groupingReason.GroupingReasonId}' already exists");
            }

            return Task.FromResult(groupingReason);
        }

        public Task<ReleasedProvider> CreateReleasedProvider(ReleasedProvider releasedProvider)
        {
            throw new NotImplementedException();
        }

        public Task<ReleasedProviderChannelVariationReason> CreateReleasedProviderChannelVariationReason(ReleasedProviderChannelVariationReason reason)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ReleasedProviderChannelVariationReason>> CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(IEnumerable<ReleasedProviderChannelVariationReason> variationReason)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ReleasedProvider>> CreateReleasedProvidersUsingAmbientTransaction(IEnumerable<ReleasedProvider> releasedProviders)
        {
            List<ReleasedProvider> result = new List<ReleasedProvider>();

            foreach (var releasedProvider in releasedProviders)
            {
                if (releasedProvider.ReleasedProviderId <= 0)
                {
                    releasedProvider.ReleasedProviderId = _releasedProvidersNextId++;
                }

                EnsureSpecificationExists(releasedProvider.SpecificationId);

                if (!_releasedProviders.TryAdd(releasedProvider.ReleasedProviderId, releasedProvider))
                {
                    throw new InvalidOperationException($"Release provider already exists with ID '{releasedProvider.ReleasedProviderId}'");
                }

                result.Add(releasedProvider);
            }

            return Task.FromResult(result.AsEnumerable());
        }

        private void EnsureSpecificationExists(string specificationId)
        {
            if (!_specifications.ContainsKey(specificationId))
            {
                throw new InvalidOperationException($"Specification with ID '{specificationId}' does not exist");
            }
        }

        public Task<ReleasedProviderVersion> CreateReleasedProviderVersion(ReleasedProviderVersion releasedProviderVersion)
        {
            throw new NotImplementedException();
        }

        public Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannel(ReleasedProviderVersionChannel providerVersionChannel)
        {
            throw new NotImplementedException();
        }

        public Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannelsUsingAmbientTransaction(ReleasedProviderVersionChannel providerVersionChannel)
        {
            if (providerVersionChannel.ReleasedProviderVersionChannelId <= 0)
            {
                providerVersionChannel.ReleasedProviderVersionChannelId = _releasedProviderVersionChannelsNextId++;
            }

            EnsureChannelExists(providerVersionChannel.ChannelId);
            EnsureReleaseProviderVersionExists(providerVersionChannel.ReleasedProviderVersionId);

            if (!_releasedProviderVersionChannels.TryAdd(providerVersionChannel.ReleasedProviderVersionChannelId, providerVersionChannel))
            {
                throw new InvalidOperationException($"A released provider version channel already exists with ID '{providerVersionChannel.ReleasedProviderVersionChannelId}'");
            }

            return Task.FromResult(providerVersionChannel);
        }

        private void EnsureReleaseProviderVersionExists(int releasedProviderVersionId)
        {
            if (!_releasedProviderVersions.ContainsKey(releasedProviderVersionId))
            {
                throw new InvalidOperationException($"Release provider version with ID '{releasedProviderVersionId}' does not exist");
            }
        }

        private void EnsureChannelExists(int channelId)
        {
            if (!_channels.Values.Any(_ => _.ChannelId == channelId))
            {
                throw new InvalidOperationException($"Channel with ID '{channelId}' does not exist");
            }
        }

        public Task<ReleasedProviderVersion> CreateReleasedProviderVersionsUsingAmbientTransaction(ReleasedProviderVersion providerVersion)
        {

            if (providerVersion.ReleasedProviderVersionId <= 0)
            {
                providerVersion.ReleasedProviderVersionId = _releasedProviderVersionsNextId++;
            }

            EnsureReleasedProviderExists(providerVersion.ReleasedProviderId);

            if (!_releasedProviderVersions.TryAdd(providerVersion.ReleasedProviderId, providerVersion))
            {
                throw new InvalidOperationException($"Release provider already exists with ID '{providerVersion.ReleasedProviderVersionId}'");
            }


            return Task.FromResult(providerVersion);
        }

        private void EnsureReleasedProviderExists(int releasedProviderId)
        {
            if (!_releasedProviders.ContainsKey(releasedProviderId))
            {
                throw new InvalidOperationException($"Released provider does not exist with ID '{releasedProviderId}'");
            }
        }

        public Task<Specification> CreateSpecification(Specification specification)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));

            if (!_specifications.TryAdd(specification.SpecificationId, specification))
            {
                throw new InvalidOperationException("Specification already exists");
            }

            return Task.FromResult(specification);
        }

        public Task<Specification> CreateSpecificationUsingAmbientTransaction(Specification specification)
        {
            throw new NotImplementedException();
        }

        public Task<VariationReason> CreateVariationReason(VariationReason reason)
        {
            if (reason.VariationReasonId <= 0)
            {
                reason.VariationReasonId = _variationReasonsNextId++;
            }

            if (!_variationReasons.TryAdd(reason.VariationReasonId, reason))
            {
                throw new InvalidOperationException($"A variation reason with this ID '{reason.VariationReasonId}' already exists");
            }

            return Task.FromResult(reason);
        }

        public Task<Channel> GetChannelByChannelCode(string channelCode)
        {
            throw new NotImplementedException();
        }

        public Task<int?> GetChannelIdFromUrlKey(string normalisedKey)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Channel>> GetChannels()
        {
            return Task.FromResult(_channels.Values.AsEnumerable());
        }

        public Task<FundingGroup> GetFundingGroup(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue)
        {
            FundingGroup fundingGroup = _fundingGroups.Values
                .SingleOrDefault(_ => _.ChannelId == channelId
                && _.SpecificationId == specificationId
                && _.GroupingReasonId == groupingReasonId
                && _.OrganisationGroupTypeClassification == organisationGroupTypeClassification
                && _.OrganisationGroupIdentifierValue == organisationGroupIdentifierValue);

            return Task.FromResult(fundingGroup);
        }

        public Task<IEnumerable<string>> GetFundingGroupIdsForProviderFunding(int channelId, string publishedProviderVersion)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FundingGroup>> GetFundingGroups()
        {
            throw new NotImplementedException();
        }

        public Task<FundingGroupVersion> GetFundingGroupVersion(int fundingGroupId, int majorVersion)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersions()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionsBySpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public async Task<FundingPeriod> GetFundingPeriodByCode(string code)
        {
            _fundingPeriods.TryGetValue(code, out FundingPeriod fundingPeriod);
            return await Task.FromResult(fundingPeriod);
        }

        public Task<IEnumerable<FundingPeriod>> GetFundingPeriods()
        {
            return Task.FromResult(_fundingPeriods.Values.AsEnumerable());
        }

        public async Task<FundingStream> GetFundingStreamByCode(string code)
        {
            _fundingStreams.TryGetValue(code, out FundingStream fundingStream);
            return await Task.FromResult(fundingStream);
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            return Task.FromResult(_fundingStreams.Values.AsEnumerable());
        }

        public Task<IEnumerable<GroupingReason>> GetGroupingReasons()
        {
            return Task.FromResult(_groupingReasons.Values.AsEnumerable());
        }

        public Task<IEnumerable<FundingGroupVersion>> GetLatestFundingGroupVersionsBySpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LatestProviderVersionInFundingGroup>> GetLatestProviderVersionInFundingGroups(string id, int channelId)
        {
            return Task.FromResult(Enumerable.Empty<LatestProviderVersionInFundingGroup>());
        }

        public Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersions(string specificationId, IEnumerable<int> channelIds)
        {
            return Task.FromResult(Enumerable.Empty<ProviderVersionInChannel>());
        }

        public Task<IEnumerable<ReleasedProviderSummary>> GetLatestReleasedProviderSummaryBySpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ReleasedDataAllocationHistory>> GetPublishedProviderTransactionHistory(string specificationId, string providerId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProviders()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ReleasedProviderSummary>> GetReleasedProviderSummaryBySpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ReleasedProviderVersionChannel> GetReleasedProviderVersionChannel(int releasedProviderVersionId, int channelId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ReleasedProviderVersion>> GetReleasedProviderVersions()
        {
            throw new NotImplementedException();
        }

        public Task<Specification> GetSpecificationById(string id)
        {
            Specification result = null;
            _specifications.TryGetValue(id, out result);

            return Task.FromResult(result);
        }

        public Task<IEnumerable<Specification>> GetSpecifications()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<VariationReason>> GetVariationReasons()
        {
            return Task.FromResult(_variationReasons.Values.AsEnumerable());
        }

        public void InitialiseTransaction()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ExternalFeedFundingGroupItem>> QueryPublishedFunding(int channelId, IEnumerable<string> fundingStreamIds, IEnumerable<string> fundingPeriodIds, IEnumerable<string> groupingReasons, IEnumerable<string> variationReasons, int top, int? pageRef, int totalCount)
        {
            throw new NotImplementedException();
        }

        public Task<int> QueryPublishedFundingCount(int channelId, IEnumerable<string> fundingStreamIds, IEnumerable<string> fundingPeriodIds, IEnumerable<string> groupingReasons, IEnumerable<string> variationReasons)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateChannel(Channel channel)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateSpecificationUsingAmbientTransaction(Specification specification)
        {
            throw new NotImplementedException();
        }
    }
}
