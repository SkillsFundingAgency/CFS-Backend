using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Services.Publishing.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryReleaseManagementRepository : IReleaseManagementRepository
    {
        readonly ConcurrentDictionary<int, FundingPeriod> _fundingPeriods = new();
        readonly ConcurrentDictionary<int, FundingStream> _fundingStreams = new();
        readonly ConcurrentDictionary<string, Specification> _specifications = new();
        readonly ConcurrentDictionary<int, GroupingReason> _groupingReasons = new();
        readonly ConcurrentDictionary<int, VariationReason> _variationReasons = new();

        readonly ConcurrentDictionary<string, Channel> _channels = new();

        readonly ConcurrentDictionary<Guid, FundingGroup> _fundingGroups = new();
        readonly ConcurrentDictionary<Guid, FundingGroupVersion> _fundingGroupVersions = new();
        readonly ConcurrentDictionary<Guid, FundingGroupVersionVariationReason> _fundingGroupVersionsVariationReasons = new();

        readonly ConcurrentDictionary<Guid, FundingGroupProvider> _fundingGroupProviders = new();

        readonly ConcurrentDictionary<Guid, ReleasedProvider> _releasedProviders = new();
        readonly ConcurrentDictionary<Guid, ReleasedProviderVersion> _releasedProviderVersions = new();
        readonly ConcurrentDictionary<Guid, ReleasedProviderVersionChannel> _releasedProviderVersionChannels = new();
        readonly ConcurrentDictionary<Guid, ReleasedProviderChannelVariationReason> _releasedProviderVersionChannelVariatonReasons = new();

        int _variationReasonsNextId = 1;

        public InMemoryReleaseManagementRepository()
        {
        }

        public int ReleasedProviderVersionChannelCount { get { return _releasedProviderVersionChannels.Count; } }
        public int ReleasedProviderCount { get { return _releasedProviders.Count; } }
        public int ReleasedProviderVersionsCount { get { return _releasedProviderVersions.Count; } }
        public int ReleasedProviderVersionVariationReasonsCount { get { return _releasedProviderVersionChannelVariatonReasons.Count; } }
        public int FundingGroupsCount { get { return _fundingGroups.Count; } }
        public int FundingGroupsVersionsCount { get { return _fundingGroupVersions.Count; } }
        public int FundingGroupsVariationReasonsCount { get { return _fundingGroupVersionsVariationReasons.Count; } }
        public int FundingGroupProvidersCount { get { return _fundingGroupProviders.Count; } }

        public Task ClearDatabase()
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public void RollBack()
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
        public async Task<IEnumerable<FundingChannelVersion>> GetFundingGroupVersionsForSpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<FundingChannelVersion>> GetReleaseProviderVersionsForSpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }
        public Task<FundingGroup> CreateFundingGroup(FundingGroup fundingGroup)
        {
            EnsureIdNotEmpty(fundingGroup.FundingGroupId, "Funding group");
            CheckUniqueConstraints(fundingGroup);

            if (!_fundingGroups.TryAdd(fundingGroup.FundingGroupId, fundingGroup))
            {
                throw new InvalidOperationException($"Funding group with id '{fundingGroup.FundingGroupId}' already exists");
            }

            return Task.FromResult(fundingGroup);
        }

        private void EnsureIdNotEmpty(Guid id, string entityName)
        {
            if (id == Guid.Empty)
            {
                throw new InvalidOperationException($"{entityName} primary key identifier must not be empty");
            }
        }

        private void CheckUniqueConstraints(FundingGroup fundingGroup)
        {
            bool existingGroup = _fundingGroups.Values.Where(_ =>
                _.OrganisationGroupIdentifierValue == fundingGroup.OrganisationGroupIdentifierValue
                && _.ChannelId == fundingGroup.ChannelId
                && _.GroupingReasonId == fundingGroup.GroupingReasonId
                && _.OrganisationGroupTypeCode == fundingGroup.OrganisationGroupTypeCode
                ).Any();

            if (existingGroup)
            {
                string outputObject = JsonConvert.SerializeObject(fundingGroup, Formatting.Indented);
                throw new InvalidOperationException($"A group already exists with this composite key: {outputObject}");
            }
        }

        public Task<FundingGroupProvider> CreateFundingGroupProvider(FundingGroupProvider fundingGroupProvider)
        {
            EnsureIdNotEmpty(fundingGroupProvider.FundingGroupProviderId, "Funding group provider");
            EnsureIdNotEmpty(fundingGroupProvider.FundingGroupVersionId, "Funding group version");
            EnsureIdNotEmpty(fundingGroupProvider.ReleasedProviderVersionChannelId, "Released provider version channel");

            EnsureFundingGroupVersionExists(fundingGroupProvider.FundingGroupVersionId);
            EnsureReleaseProviderVersionChannelExists(fundingGroupProvider.ReleasedProviderVersionChannelId);

            if (!_fundingGroupProviders.TryAdd(fundingGroupProvider.FundingGroupProviderId, fundingGroupProvider))
            {
                throw new InvalidOperationException($"Funding group provider already exists with ID '{fundingGroupProvider.FundingGroupProviderId}'");
            }

            return Task.FromResult(fundingGroupProvider);
        }

        public Task<FundingGroupProvider> CreateFundingGroupProviderUsingAmbientTransaction(FundingGroupProvider fundingGroupProvider)
        {
            EnsureIdNotEmpty(fundingGroupProvider.FundingGroupProviderId, "Funding group provider");
            EnsureIdNotEmpty(fundingGroupProvider.FundingGroupVersionId, "Funding group version");
            EnsureIdNotEmpty(fundingGroupProvider.ReleasedProviderVersionChannelId, "Released provider version channel");

            EnsureFundingGroupVersionExists(fundingGroupProvider.FundingGroupVersionId);
            EnsureReleaseProviderVersionChannelExists(fundingGroupProvider.ReleasedProviderVersionChannelId);

            if (!_fundingGroupProviders.TryAdd(fundingGroupProvider.FundingGroupProviderId, fundingGroupProvider))
            {
                throw new InvalidOperationException($"Funding group provider already exists with ID '{fundingGroupProvider.FundingGroupProviderId}'");
            }

            return Task.FromResult(fundingGroupProvider);
        }

        private void EnsureReleaseProviderVersionChannelExists(Guid releasedProviderVersionChannelId)
        {
            if (!_releasedProviderVersionChannels.ContainsKey(releasedProviderVersionChannelId))
            {
                throw new InvalidOperationException($"Released provider version channel with ID '{releasedProviderVersionChannelId}' does not exist");
            }
        }

        public Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReason(FundingGroupVersionVariationReason reason)
        {
            EnsureIdNotEmpty(reason.FundingGroupVersionVariationReasonId, "Funding group variation reason");
            EnsureIdNotEmpty(reason.FundingGroupVersionId, "Funding group version");

            EnsureFundingGroupVersionExists(reason.FundingGroupVersionId);
            EnsureVariationReasonExists(reason.VariationReasonId);

            if (!_fundingGroupVersionsVariationReasons.TryAdd(reason.FundingGroupVersionVariationReasonId, reason))
            {
                throw new InvalidOperationException($"Funding group version variation reason with id '{reason.FundingGroupVersionVariationReasonId}' already exists");
            }

            return Task.FromResult(reason);
        }

        public Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReasonUsingAmbientTransaction(FundingGroupVersionVariationReason reason)
        {
            EnsureIdNotEmpty(reason.FundingGroupVersionVariationReasonId, "Funding group variation reason");
            EnsureIdNotEmpty(reason.FundingGroupVersionId, "Funding group version");

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

        private void EnsureFundingGroupVersionExists(Guid fundingGroupVersionId)
        {
            if (!_fundingGroupVersions.ContainsKey(fundingGroupVersionId))
            {
                throw new InvalidOperationException($"Funding group version ID '{fundingGroupVersionId}' not found");
            }
        }

        public Task<FundingGroupVersion> CreateFundingGroupVersion(FundingGroupVersion fundingGroupVersion)
        {
            return CreateFundingGroupVersionUsingAmbientTransaction(fundingGroupVersion);
        }

        public Task<FundingGroupVersion> CreateFundingGroupVersionUsingAmbientTransaction(FundingGroupVersion fundingGroupVersion)
        {
            EnsureIdNotEmpty(fundingGroupVersion.FundingGroupVersionId, "Funding group version");
            EnsureIdNotEmpty(fundingGroupVersion.FundingGroupId, "Funding group");

            EnsureFundingGroupExists(fundingGroupVersion.FundingGroupId);

            if (!_fundingGroupVersions.TryAdd(fundingGroupVersion.FundingGroupVersionId, fundingGroupVersion))
            {
                throw new InvalidOperationException($"Funding group version with ID '{fundingGroupVersion.FundingGroupVersionId}' already exists");
            }

            return Task.FromResult(fundingGroupVersion);
        }

        public ReleasedProvider GetReleasedProviderById(Guid releasedProviderId)
        {
            return _releasedProviders[releasedProviderId];
        }

        public Task<ReleasedProviderVersion> GetReleasedProviderVersionById(Guid releasedProviderId)
        {
            _releasedProviderVersions.TryGetValue(releasedProviderId, out ReleasedProviderVersion rpv);

            return Task.FromResult(rpv);
        }

        private void EnsureFundingGroupExists(Guid fundingGroupId)
        {
            if (!_fundingGroups.ContainsKey(fundingGroupId))
            {
                throw new InvalidOperationException($"Funding group ID '{fundingGroupId}' not found");
            }
        }

        public Task<FundingPeriod> CreateFundingPeriod(FundingPeriod fundingPeriod)
        {
            _fundingPeriods.TryAdd(fundingPeriod.FundingPeriodId, fundingPeriod);

            return Task.FromResult(fundingPeriod);
        }

        public Task<FundingPeriod> CreateFundingPeriodUsingAmbientTransaction(FundingPeriod fundingPeriod)
        {
            throw new NotImplementedException();
        }

        public Task<FundingStream> CreateFundingStream(FundingStream fundingStream)
        {
            _fundingStreams.TryAdd(fundingStream.FundingStreamId, fundingStream);
            return Task.FromResult(fundingStream);
        }

        public Task<FundingStream> CreateFundingStreamUsingAmbientTransaction(FundingStream fundingStream)
        {
            throw new NotImplementedException();
        }

        public Task<GroupingReason> CreateGroupingReason(GroupingReason groupingReason)
        {
            if (_groupingReasons.Values.Any(_ => _.GroupingReasonCode == groupingReason.GroupingReasonCode))
            {
                throw new InvalidOperationException($"Grouping reason with code '{groupingReason.GroupingReasonCode}' already exists");
            }

            if (!_groupingReasons.TryAdd(groupingReason.GroupingReasonId, groupingReason))
            {
                throw new InvalidOperationException($"Grouping reason with ID '{groupingReason.GroupingReasonId}' already exists");
            }

            return Task.FromResult(groupingReason);
        }

        public async Task<ReleasedProvider> CreateReleasedProvider(ReleasedProvider releasedProvider)
        {
            return (await CreateReleasedProvidersUsingAmbientTransaction(new[] { releasedProvider })).First();
        }

        public Task<ReleasedProviderChannelVariationReason> GetReleasedProviderVersionChannelVariationReason(Guid releasedProviderChannelVariationReasonId)
        {
            _releasedProviderVersionChannelVariatonReasons.TryGetValue(releasedProviderChannelVariationReasonId, out ReleasedProviderChannelVariationReason releasedProviderChannelVariationReason);

            return Task.FromResult(releasedProviderChannelVariationReason);
        }

        public Task<ReleasedProviderChannelVariationReason> CreateReleasedProviderChannelVariationReason(ReleasedProviderChannelVariationReason reason)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ReleasedProviderChannelVariationReason>> CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(IEnumerable<ReleasedProviderChannelVariationReason> providerVariations)
        {
            List<ReleasedProviderChannelVariationReason> results = new();

            foreach (ReleasedProviderChannelVariationReason providerVariation in providerVariations)
            {
                EnsureIdNotEmpty(providerVariation.ReleasedProviderChannelVariationReasonId, "Provider channel variation reason");
                EnsureIdNotEmpty(providerVariation.ReleasedProviderVersionChannelId, "Provider channel version channel");

                EnsureReleaseProviderVersionChannelExists(providerVariation.ReleasedProviderVersionChannelId);
                EnsureVariationReasonExists(providerVariation.VariationReasonId);

                if (!_releasedProviderVersionChannelVariatonReasons.TryAdd(providerVariation.ReleasedProviderChannelVariationReasonId, providerVariation))
                {
                    throw new InvalidOperationException($"Provider channel variation reason with ID '{providerVariation.ReleasedProviderVersionChannelId}' already exists");
                }

                results.Add(providerVariation);
            }

            return Task.FromResult(results.AsEnumerable());
        }

        public Task<IEnumerable<ReleasedProvider>> CreateReleasedProvidersUsingAmbientTransaction(IEnumerable<ReleasedProvider> releasedProviders)
        {
            List<ReleasedProvider> result = new List<ReleasedProvider>();

            foreach (ReleasedProvider releasedProvider in releasedProviders)
            {
                EnsureIdNotEmpty(releasedProvider.ReleasedProviderId, "Released provider");
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

        internal Task<FundingGroup> GetFundingGroup(Guid fundingGroupId)
        {
            _fundingGroups.TryGetValue(fundingGroupId, out FundingGroup group);

            return Task.FromResult(group);
        }

        public async Task<ReleasedProviderVersion> CreateReleasedProviderVersion(ReleasedProviderVersion releasedProviderVersion)
        {
            return await CreateReleasedProviderVersionUsingAmbientTransaction(releasedProviderVersion);
        }

        public Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannel(ReleasedProviderVersionChannel providerVersionChannel)
        {
            return CreateReleasedProviderVersionChannelsUsingAmbientTransaction(providerVersionChannel);
        }

        public Task<ReleasedProviderVersionChannel> GetReleasedProviderVersionChannel(Guid releasedProviderVersionChannelId)
        {
            _releasedProviderVersionChannels.TryGetValue(releasedProviderVersionChannelId, out ReleasedProviderVersionChannel rpvc);

            return Task.FromResult(rpvc);
        }

        public Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannelsUsingAmbientTransaction(ReleasedProviderVersionChannel providerVersionChannel)
        {
            EnsureIdNotEmpty(providerVersionChannel.ReleasedProviderVersionChannelId, "Released provider version channel");
            EnsureIdNotEmpty(providerVersionChannel.ReleasedProviderVersionId, "Released provider version");

            EnsureChannelExists(providerVersionChannel.ChannelId);
            EnsureReleaseProviderVersionExists(providerVersionChannel.ReleasedProviderVersionId);

            if (!_releasedProviderVersionChannels.TryAdd(providerVersionChannel.ReleasedProviderVersionChannelId, providerVersionChannel))
            {
                throw new InvalidOperationException($"A released provider version channel already exists with ID '{providerVersionChannel.ReleasedProviderVersionChannelId}'");
            }

            return Task.FromResult(providerVersionChannel);
        }

        internal Task<FundingGroupVersion> GetFundingGroupVersion(Guid fundingGroupVersionId)
        {
            _fundingGroupVersions.TryGetValue(fundingGroupVersionId, out FundingGroupVersion fundingGroupVersion);
            return Task.FromResult(fundingGroupVersion);
        }

        private void EnsureReleaseProviderVersionExists(Guid releasedProviderVersionId)
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

        public Task<ReleasedProviderVersion> CreateReleasedProviderVersionUsingAmbientTransaction(ReleasedProviderVersion providerVersion)
        {
            EnsureIdNotEmpty(providerVersion.ReleasedProviderId, "Released provider");
            EnsureIdNotEmpty(providerVersion.ReleasedProviderVersionId, "Released provider version");

            EnsureReleasedProviderExists(providerVersion.ReleasedProviderId);
            EnsureNoMajorVersionDuplicatesWithinProvider(providerVersion);

            if (!_releasedProviderVersions.TryAdd(providerVersion.ReleasedProviderVersionId, providerVersion))
            {
                throw new InvalidOperationException($"Released provider version already exists with ID '{providerVersion.ReleasedProviderVersionId}'");
            }

            return Task.FromResult(providerVersion);
        }

        private void EnsureNoMajorVersionDuplicatesWithinProvider(ReleasedProviderVersion providerVersion)
        {
            if (_releasedProviderVersions.Values.Any(_ => _.ReleasedProviderId == providerVersion.ReleasedProviderId && _.MajorVersion == providerVersion.MajorVersion))
            {
                throw new InvalidOperationException($"A provider version already exists for provider ID '{providerVersion.ReleasedProviderId}' with major version '{providerVersion.MajorVersion}'");
            }
        }

        public Task<FundingGroupVersionVariationReason> GetFundingGroupVersionVariationReason(Guid fundingGroupVersionVariationReasonId)
        {
            _fundingGroupVersionsVariationReasons.TryGetValue(fundingGroupVersionVariationReasonId, out FundingGroupVersionVariationReason fundingGroupVersionVariationReason);

            return Task.FromResult(fundingGroupVersionVariationReason);
        }

        private void EnsureReleasedProviderExists(Guid releasedProviderId)
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

        public Task<FundingGroupProvider> GetFundingGroupProviderById(Guid fundingGroupProviderId)
        {
            _fundingGroupProviders.TryGetValue(fundingGroupProviderId, out FundingGroupProvider fundingGroupProvider);

            return Task.FromResult(fundingGroupProvider);
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
            Channel channel = _channels.Values.SingleOrDefault(_ => _.ChannelCode == channelCode);

            return Task.FromResult(channel);
        }

        public Task<IEnumerable<Channel>> GetChannels()
        {
            return Task.FromResult(_channels.Values.AsEnumerable());
        }

        public Task<FundingGroup> GetFundingGroupUsingAmbientTransaction(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue)
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
            return Task.FromResult(_fundingGroups.Values.AsEnumerable());
        }

        public Task<FundingGroupVersion> GetFundingGroupVersion(int fundingGroupId, int majorVersion)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersions()
        {
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersionsByChannelIdUsingAmbientTransaction(string specificationId, IEnumerable<int> channelIds)
        {
            return await GetLatestPublishedProviderVersionsByChannelId(specificationId, channelIds);
        }
        public Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionsBySpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public async Task<FundingPeriod> GetFundingPeriodByCode(string code)
        {
            FundingPeriod fundingPeriod = _fundingPeriods.Values.SingleOrDefault(_ => _.FundingPeriodCode == code);
            return await Task.FromResult(fundingPeriod);
        }

        public Task<IEnumerable<FundingPeriod>> GetFundingPeriods()
        {
            return Task.FromResult(_fundingPeriods.Values.AsEnumerable());
        }

        public async Task<FundingStream> GetFundingStreamByCode(string code)
        {
            FundingStream fundingStream = _fundingStreams.Values.SingleOrDefault(_ => _.FundingStreamCode == code);
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
            Dictionary<Guid, ReleasedProvider> releasedProvidersForSpecification = _releasedProviders.Values
                    .Where(_ => _.SpecificationId == specificationId)
                    .ToDictionary(_ => _.ReleasedProviderId);

            Guid[] releasedProviderIds = releasedProvidersForSpecification.Values.Select(_ => _.ReleasedProviderId).ToArray();

            // ReleasedProviderVersionId is the key
            IEnumerable<ReleasedProviderVersion> releasedProviderVersionsInSpec = _releasedProviderVersions.Values.Where(_ => releasedProviderIds.Contains(_.ReleasedProviderId));

            IEnumerable<ReleasedProviderVersionChannel> providerVersionsInChannelsSpecified =
                _releasedProviderVersionChannels.Values
                    .Where(_ => channelIds.Contains(_.ChannelId));

            Dictionary<int, Dictionary<Guid, ProviderVersionInChannel>> results = new Dictionary<int, Dictionary<Guid, ProviderVersionInChannel>>();

            Dictionary<int, Channel> channels = _channels.Values.ToDictionary(_ => _.ChannelId);


            foreach (var rpvc in providerVersionsInChannelsSpecified)
            {
                ReleasedProviderVersion releasedProviderVersion = _releasedProviderVersions[rpvc.ReleasedProviderVersionId];
                ReleasedProvider releasedProvider = _releasedProviders[releasedProviderVersion.ReleasedProviderId];

                if (!results.TryGetValue(rpvc.ChannelId, out Dictionary<Guid, ProviderVersionInChannel> channelResults))
                {
                    channelResults = new Dictionary<Guid, ProviderVersionInChannel>();
                    results.Add(rpvc.ChannelId, channelResults);
                }

                if (channelResults.ContainsKey(releasedProvider.ReleasedProviderId))
                {
                    // Already processed this provider, the latest version is added to the results return dictionary
                    continue;
                }

                ReleasedProviderVersion latestProviderVersionInChannel = releasedProviderVersionsInSpec.Where(
                    _ => _.ReleasedProviderId == releasedProvider.ReleasedProviderId)
                    .OrderByDescending(_ => _.MajorVersion).FirstOrDefault();

                if (latestProviderVersionInChannel == null)
                {
                    continue;
                }

                // Check to determine if this version is the latest, else wait until that version is the latest
                if (latestProviderVersionInChannel.ReleasedProviderVersionId != rpvc.ReleasedProviderVersionId)
                {
                    continue;
                }

                Channel channel = channels[rpvc.ChannelId];

                ProviderVersionInChannel providerVersionInChannel = new ProviderVersionInChannel()
                {
                    ChannelCode = channel.ChannelCode,
                    ChannelId = channel.ChannelId,
                    ChannelName = channel.ChannelName,
                    CoreProviderVersionId = latestProviderVersionInChannel.CoreProviderVersionId,
                    MajorVersion = latestProviderVersionInChannel.MajorVersion,
                    MinorVersion = latestProviderVersionInChannel.MinorVersion,
                    ProviderId = _releasedProviders[latestProviderVersionInChannel.ReleasedProviderId].ProviderId,
                    ReleasedProviderVersionChannelId = rpvc.ReleasedProviderVersionChannelId,
                };

                channelResults.Add(latestProviderVersionInChannel.ReleasedProviderId, providerVersionInChannel);
            }


            return Task.FromResult(results.SelectMany(_ => _.Value.Values));
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
            return Task.FromResult(_releasedProviders.Values.AsEnumerable());
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
            return Task.FromResult(_releasedProviderVersions.Values.AsEnumerable());
        }

        public Task<Specification> GetSpecificationById(string id)
        {
            Specification result = null;
            _specifications.TryGetValue(id, out result);

            return Task.FromResult(result);
        }

        public Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersionsByChannelId(string specificationId, IEnumerable<int> channelIds, ISqlTransaction transaction = null)
        {
            Dictionary<Guid, ReleasedProvider> releasedProvidersForSpecification = _releasedProviders.Values
                    .Where(_ => _.SpecificationId == specificationId)
                    .ToDictionary(_ => _.ReleasedProviderId);

            Guid[] releasedProviderIds = releasedProvidersForSpecification.Values.Select(_ => _.ReleasedProviderId).ToArray();

            // ReleasedProviderVersionId is the key
            IEnumerable<ReleasedProviderVersion> releasedProviderVersionsInSpec = _releasedProviderVersions.Values.Where(_ => releasedProviderIds.Contains(_.ReleasedProviderId));

            IEnumerable<ReleasedProviderVersionChannel> providerVersionsInChannelsSpecified =
                _releasedProviderVersionChannels.Values
                    .Where(_ => channelIds.Contains(_.ChannelId));

            Dictionary<int, Dictionary<Guid, ProviderVersionInChannel>> results = new Dictionary<int, Dictionary<Guid, ProviderVersionInChannel>>();

            Dictionary<int, Channel> channels = _channels.Values.ToDictionary(_ => _.ChannelId);


            foreach (var rpvc in providerVersionsInChannelsSpecified)
            {
                ReleasedProviderVersion releasedProviderVersion = _releasedProviderVersions[rpvc.ReleasedProviderVersionId];
                ReleasedProvider releasedProvider = _releasedProviders[releasedProviderVersion.ReleasedProviderId];

                if (!results.TryGetValue(rpvc.ChannelId, out Dictionary<Guid, ProviderVersionInChannel> channelResults))
                {
                    channelResults = new Dictionary<Guid, ProviderVersionInChannel>();
                    results.Add(rpvc.ChannelId, channelResults);
                }

                if (channelResults.ContainsKey(releasedProvider.ReleasedProviderId))
                {
                    // Already processed this provider, the latest version is added to the results return dictionary
                    continue;
                }

                ReleasedProviderVersion latestProviderVersionInChannel = releasedProviderVersionsInSpec.Where(
                    _ => _.ReleasedProviderId == releasedProvider.ReleasedProviderId)
                    .OrderByDescending(_ => _.MajorVersion).FirstOrDefault();

                if (latestProviderVersionInChannel == null)
                {
                    continue;
                }

                // Check to determine if this version is the latest, else wait until that version is the latest
                if (latestProviderVersionInChannel.ReleasedProviderVersionId != rpvc.ReleasedProviderVersionId)
                {
                    continue;
                }

                Channel channel = channels[rpvc.ChannelId];

                ProviderVersionInChannel providerVersionInChannel = new ProviderVersionInChannel()
                {
                    ChannelCode = channel.ChannelCode,
                    ChannelId = channel.ChannelId,
                    ChannelName = channel.ChannelName,
                    CoreProviderVersionId = latestProviderVersionInChannel.CoreProviderVersionId,
                    MajorVersion = latestProviderVersionInChannel.MajorVersion,
                    MinorVersion = latestProviderVersionInChannel.MinorVersion,
                    ProviderId = _releasedProviders[latestProviderVersionInChannel.ReleasedProviderId].ProviderId,
                    ReleasedProviderVersionChannelId = rpvc.ReleasedProviderVersionChannelId,
                };

                channelResults.Add(latestProviderVersionInChannel.ReleasedProviderId, providerVersionInChannel);
            }


            return Task.FromResult(results.SelectMany(_ => _.Value.Values));
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

        public Task<Channel> GetChannelFromUrlKey(string normalisedKey)
        {
            throw new NotImplementedException();
        }

        public Task<ProviderVersionInChannel> GetReleasedProvider(string publishedProviderVersion, int channelId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LatestFundingGroupVersion>> GetLatestFundingGroupMajorVersionsBySpecificationId(string specificationId, int channelId)
        {
            IEnumerable<Guid> fundingGroupsForSpecification =
                _fundingGroups.Values
                .Where(_ => _.SpecificationId == specificationId)
                .Select(_ => _.FundingGroupId);

            IEnumerable<FundingGroupVersion> fundingGroupVersionsForSpecification =
                _fundingGroupVersions.Values
                .Where(_ => fundingGroupsForSpecification.Contains(_.FundingGroupId)
                    && _.ChannelId == channelId);

            IEnumerable<LatestFundingGroupVersion> latestVersions =
                fundingGroupVersionsForSpecification
                .GroupBy(_ => _.FundingGroupId)
                .Select(_ => _.OrderByDescending(g => g.MajorVersion).First())
                .Select(_ => new LatestFundingGroupVersion()
                {
                    MajorVersion = _.MajorVersion,
                    FundingGroupVersionId = _.FundingGroupVersionId,
                    FundingPeriodCode = _fundingPeriods[_.FundingPeriodId].FundingPeriodCode,
                    FundingStreamCode = _fundingStreams[_.FundingStreamId].FundingStreamCode,
                    FundingGroupId = _.FundingGroupId,
                    GroupingReasonCode = _groupingReasons[_.GroupingReasonId].GroupingReasonCode,
                    OrganisationGroupIdentifierValue = _fundingGroups[_.FundingGroupId].OrganisationGroupIdentifierValue,
                    OrganisationGroupTypeCode = _fundingGroups[_.FundingGroupId].OrganisationGroupTypeCode,
                });

            return Task.FromResult(latestVersions);
        }

        public Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersionsUsingAmbientTransaction(string specificationId, IEnumerable<int> channelIds)
        {
            return GetLatestPublishedProviderVersions(specificationId, channelIds);
        }

        public Task<FundingGroup> CreateFundingGroupUsingAmbientTransaction(FundingGroup fundingGroup)
        {
            return CreateFundingGroup(fundingGroup);
        }

        public async Task<IEnumerable<FundingGroup>> BulkCreateFundingGroupsUsingAmbientTransaction(IEnumerable<FundingGroup> fundingGroups)
        {
            List<FundingGroup> results = new();

            foreach (FundingGroup fundingGroup in fundingGroups)
            {
                results.Add(await CreateFundingGroup(fundingGroup));
            }

            return results;
        }

        public Task<IEnumerable<FundingGroup>> GetFundingGroupsBySpecificationAndChannelUsingAmbientTransaction(string specificationId, int channelId)
        {
            return Task.FromResult(_fundingGroups
                .Select(_ => _.Value)
                .Where(_ => _.SpecificationId == specificationId && _.ChannelId == channelId)
                .AsEnumerable());
        }

        public async Task<IEnumerable<FundingGroupVersion>> BulkCreateFundingGroupVersionsUsingAmbientTransaction(IEnumerable<FundingGroupVersion> fundingGroupVersions)
        {
            List<FundingGroupVersion> results = new();

            foreach (FundingGroupVersion fundingGroupVersion in fundingGroupVersions)
            {
                results.Add(await CreateFundingGroupVersion(fundingGroupVersion));
            }

            return results;
        }

        public async Task<IEnumerable<FundingGroupVersionVariationReason>> BulkCreateFundingGroupVersionVariationReasonsUsingAmbientTransaction(IEnumerable<FundingGroupVersionVariationReason> variationReasons)
        {
            List<FundingGroupVersionVariationReason> results = new();

            foreach (FundingGroupVersionVariationReason variationReason in variationReasons)
            {
                results.Add(await CreateFundingGroupVariationReason(variationReason));
            }

            return results;
        }

        public async Task<IEnumerable<FundingGroupProvider>> BulkCreateFundingGroupProvidersUsingAmbientTransaction(IEnumerable<FundingGroupProvider> providers)
        {
            List<FundingGroupProvider> results = new();

            foreach (FundingGroupProvider provider in providers)
            {
                results.Add(await CreateFundingGroupProvider(provider));
            }

            return results;
        }

        public async Task<IEnumerable<ReleasedProvider>> BulkCreateReleasedProvidersUsingAmbientTransaction(IEnumerable<ReleasedProvider> releasedProviders)
        {
            List<ReleasedProvider> results = new();

            foreach (ReleasedProvider provider in releasedProviders)
            {
                results.Add(await CreateReleasedProvider(provider));
            }

            return results;
        }

        public async Task<IEnumerable<ReleasedProviderVersion>> BulkCreateReleasedProviderVersionsUsingAmbientTransaction(IEnumerable<ReleasedProviderVersion> releasedProviderVersions)
        {
            List<ReleasedProviderVersion> results = new();

            foreach (ReleasedProviderVersion provider in releasedProviderVersions)
            {
                results.Add(await CreateReleasedProviderVersion(provider));
            }

            return results;
        }

        public async Task<IEnumerable<ReleasedProviderChannelVariationReason>> BulkCreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(IEnumerable<ReleasedProviderChannelVariationReason> variationReasons)
        {
            await CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(variationReasons);

            return variationReasons;
        }

        public async Task<IEnumerable<ReleasedProviderVersionChannel>> BulkCreateReleasedProviderVersionChannelsUsingAmbientTransaction(
            IEnumerable<ReleasedProviderVersionChannel> releasedProviderVersionChannels)
        {
            List<ReleasedProviderVersionChannel> results = new();

            foreach (ReleasedProviderVersionChannel provider in releasedProviderVersionChannels)
            {
                results.Add(await CreateReleasedProviderVersionChannelsUsingAmbientTransaction(provider));
            }

            return results;
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProviders(string specificationId)
        {
            return Task.FromResult(_releasedProviders.Values.Where(_ => _.SpecificationId == specificationId));
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProviders(string specificationId, IEnumerable<string> providerIds)
        {
            return Task.FromResult(_releasedProviders.Values
                .Where(_ =>
                    _.SpecificationId == specificationId
                    && providerIds.Contains(_.ProviderId)));
        }

        public Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersionsUsingAmbientTransaction(string specificationId)
        {
            return Task.FromResult(GetLatestReleasedProviderVersionsInternal(specificationId));
        }

        public Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersions(string specificationId)
        {
            return Task.FromResult(GetLatestReleasedProviderVersionsInternal(specificationId));
        }

        public Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersions(string specificationId, IEnumerable<string> providerIds)
        {
            return Task.FromResult(GetLatestReleasedProviderVersionsInternal(specificationId, providerIds));
        }

        public Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersionsUsingAmbientTransaction(string specificationId, IEnumerable<string> providerIds)
        {
            return Task.FromResult(GetLatestReleasedProviderVersionsInternal(specificationId, providerIds));
        }

        public async Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionChannel(Guid fundingGroupId, int channelId, ISqlTransaction transaction = null)
        {
            return (_fundingGroupVersions.Values
                    .Where(_ =>
                        _.FundingGroupId == fundingGroupId
                        && _.ChannelId == channelId));
        }
        public async Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionChannelForAllFundingId(IEnumerable<Guid> fundingGroupIds, int channelId, ISqlTransaction transaction = null)
        {
            return (_fundingGroupVersions.Values
                    .Where(_ =>
                    fundingGroupIds.Contains(_.FundingGroupId) && _.ChannelId == channelId)).GroupBy(_ => _.FundingGroupId).Select(_=>_.First());
        }
        public async Task<IEnumerable<LatestProviderVersionInFundingGroup>> GetLatestProviderVersionChannelVersionInFundingGroups(string specificationId)
        {
            return Enumerable.Empty<LatestProviderVersionInFundingGroup>();
        }
        private IEnumerable<LatestReleasedProviderVersion> GetLatestReleasedProviderVersionsInternal(string specificationId, IEnumerable<string> providerIds = null)
        {
            Dictionary<Guid, ReleasedProvider> releasedProvidersForSpecification;
            if (providerIds.AnyWithNullCheck())
            {
                releasedProvidersForSpecification = _releasedProviders.Values
                    .Where(_ => _.SpecificationId == specificationId
                            && providerIds.Contains(_.ProviderId))
                    .ToDictionary(_ => _.ReleasedProviderId);
            }
            else
            {
                releasedProvidersForSpecification = _releasedProviders.Values
                    .Where(_ => _.SpecificationId == specificationId)
                    .ToDictionary(_ => _.ReleasedProviderId);
            }

            Guid[] releasedProviderIds = releasedProvidersForSpecification.Values.Select(_ => _.ReleasedProviderId).ToArray();

            IEnumerable<ReleasedProviderVersion> allReleasedProviderVersionsForSpec = _releasedProviderVersions.Values
                    .Where(_ => releasedProviderIds.Contains(_.ReleasedProviderId));

            IEnumerable<IGrouping<Guid, ReleasedProviderVersion>> versionsByReleasedProviderId = allReleasedProviderVersionsForSpec.GroupBy(_ => _.ReleasedProviderId);

            List<LatestReleasedProviderVersion> latestReleasedProviderVersions = new List<LatestReleasedProviderVersion>();

            foreach (IGrouping<Guid, ReleasedProviderVersion> group in versionsByReleasedProviderId)
            {
                ReleasedProviderVersion latestVersion = group.MaxBy(_ => _.MajorVersion);
                latestReleasedProviderVersions.Add(new LatestReleasedProviderVersion()
                {
                    LatestMajorVersion = latestVersion.MajorVersion,
                    ProviderId = releasedProvidersForSpecification[latestVersion.ReleasedProviderId].ProviderId,
                    ReleasedProviderVersionId = latestVersion.ReleasedProviderVersionId,
                });
            }

            return latestReleasedProviderVersions;
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProvidersUsingAmbientTransaction(string specificationId)
        {
            return GetReleasedProviders(specificationId);
        }

        public Task<IEnumerable<ReleasedProvider>> GetReleasedProvidersUsingAmbientTransaction(string specificationId, IEnumerable<string> providerIds)
        {
            return GetReleasedProviders(specificationId, providerIds);
        }

        public Task<IEnumerable<GroupingReason>> GetGroupingReasonsUsingAmbientTransaction()
        {
            return GetGroupingReasons();
        }

        public Task<IEnumerable<VariationReason>> GetVariationReasonsUsingAmbientTransaction()
        {
            return GetVariationReasons();
        }

        public Task<IEnumerable<FundingPeriod>> GetFundingPeriodsUsingAmbientTransaction()
        {
            return GetFundingPeriods();
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreamsUsingAmbientTransaction()
        {
            return GetFundingStreams();
        }

        public async Task<IEnumerable<ReleasedProviderVersionChannel>> GetLatestReleasedProviderVersionsId(string specificationId, string providerIds, int channelId, ISqlTransaction transaction = null)
        {
            return null;
        }

        public Task<bool> DatabaseHasExistingFundingData(IEnumerable<string> fundingStreamIds)
        {
            throw new NotImplementedException();
        }

        public Task<ReleasedProvider> CheckIsExistingReleaseProviderId(string providerId, string specificationId)
        {
            List<string> ids = new List<string>();
            ids.Add(providerId);

            return Task.FromResult(GetReleasedProviders(specificationId, ids).Result.FirstOrDefault());
        }

        public Task<IEnumerable<ReleasedProviderVersionChannelResult>> GetLatestReleasedProviderVersionsId(string specificationId, int channelId, ISqlTransaction transaction = null)
        {
            IEnumerable<ReleasedProviderVersionChannel> versionChannels = _releasedProviderVersionChannels.Values
                    .Where(_ => _.ChannelId == channelId);

            var mapping = versionChannels.Join(_releasedProviderVersions.Values,
                rpvc => rpvc.ReleasedProviderVersionId,
                rpv => rpv.ReleasedProviderVersionId,
                (rpvc, rpv) => (rpvc, rpvc.ReleasedProviderVersionChannelId, rpv.ReleasedProviderVersionId, rpv.ReleasedProviderId))
                
                .Join(_releasedProviders.Values,
                rpvc => rpvc.ReleasedProviderId,
                rp => rp.ReleasedProviderId,
                (rpvc, rp) => (rpvc.rpvc, rpvc.ReleasedProviderVersionChannelId, rpvc.ReleasedProviderVersionId, rpvc.ReleasedProviderId, rp.ProviderId));

            return Task.FromResult(mapping.Select(_ => new ReleasedProviderVersionChannelResult
            {
                ProviderId = _.ProviderId,
                ReleasedProviderVersionChannelId = _.rpvc.ReleasedProviderVersionChannelId,
                ReleasedProviderVersionId = _.rpvc.ReleasedProviderVersionId,
                ChannelId = _.rpvc.ChannelId,
                StatusChangedDate = _.rpvc.StatusChangedDate,
                AuthorId = _.rpvc.AuthorId,
                AuthorName = _.rpvc.AuthorName,
                ChannelVersion = _.rpvc.ChannelVersion
            }));
        }
    }
}
