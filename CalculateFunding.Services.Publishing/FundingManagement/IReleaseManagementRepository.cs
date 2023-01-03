using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Services.Publishing.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseManagementRepository
    {
        void InitialiseTransaction();

        void Commit();

        void RollBack();

        Task<SqlModels.GroupingReason> CreateGroupingReason(SqlModels.GroupingReason groupingReason);

        Task<IEnumerable<SqlModels.GroupingReason>> GetGroupingReasons();
        Task<IEnumerable<SqlModels.GroupingReason>> GetGroupingReasonsUsingAmbientTransaction();

        Task<IEnumerable<VariationReason>> GetVariationReasons();
        Task<IEnumerable<VariationReason>> GetVariationReasonsUsingAmbientTransaction();

        Task<VariationReason> CreateVariationReason(VariationReason reason);

        Task<IEnumerable<Channel>> GetChannels();
        Task<Channel> GetChannelByChannelCode(string channelCode);
        Task<Channel> GetChannelFromUrlKey(string normalisedKey);
        Task<Channel> CreateChannel(Channel channel);
        Task<bool> UpdateChannel(Channel channel);
        Task<IEnumerable<FundingPeriod>> GetFundingPeriods();
        Task<IEnumerable<FundingPeriod>> GetFundingPeriodsUsingAmbientTransaction();
        Task<IEnumerable<FundingStream>> GetFundingStreams();
        Task<IEnumerable<FundingStream>> GetFundingStreamsUsingAmbientTransaction();
        Task<FundingPeriod> GetFundingPeriodByCode(string code);
        Task<FundingStream> GetFundingStreamByCode(string code);
        Task<FundingStream> CreateFundingStream(FundingStream fundingStream);
        Task<FundingPeriod> CreateFundingPeriod(FundingPeriod fundingPeriod);
        Task<FundingGroup> CreateFundingGroup(FundingGroup fundingGroup);
        Task<FundingGroup> GetFundingGroupUsingAmbientTransaction(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue);
        Task<IEnumerable<ReleasedProvider>> GetReleasedProvidersUsingAmbientTransaction(string specificationId);
        Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersionsUsingAmbientTransaction(string specificationId);
        Task<IEnumerable<ReleasedProvider>> GetReleasedProvidersUsingAmbientTransaction(string specificationId, IEnumerable<string> providerIds);
        Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersions(string specificationId, IEnumerable<string> providerIds);
        Task<IEnumerable<LatestReleasedProviderVersion>> GetLatestReleasedProviderVersionsUsingAmbientTransaction(string specificationId, IEnumerable<string> providerIds);
        Task<ReleasedProviderVersion> CreateReleasedProviderVersionUsingAmbientTransaction(ReleasedProviderVersion providerVersion);
        Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannelsUsingAmbientTransaction(ReleasedProviderVersionChannel providerVersionChannel);
        Task<IEnumerable<LatestProviderVersionInFundingGroup>> GetLatestProviderVersionInFundingGroups(string id, int channelId);
        Task<FundingGroupVersion> GetFundingGroupVersion(int fundingGroupId, int majorVersion);
        Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersions(string specificationId, IEnumerable<int> channelIds);

        Task<IEnumerable<FundingChannelVersion>> GetFundingGroupVersionsForSpecificationId(string specificationId);
        Task<IEnumerable<FundingChannelVersion>> GetReleaseProviderVersionsForSpecificationId(string specificationId);
        Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersionsUsingAmbientTransaction(string specificationId, IEnumerable<int> channelIds);
        Task<IEnumerable<LatestProviderVersionInFundingGroup>> GetLatestProviderVersionChannelVersionInFundingGroups(string specificationId);
        Task<FundingGroupVersion> CreateFundingGroupVersion(FundingGroupVersion fundingGroupVersion);
        Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReason(FundingGroupVersionVariationReason reason);
        Task<IEnumerable<Specification>> GetSpecifications();
        Task<Specification> CreateSpecification(Specification specification);
        Task<Specification> CreateSpecificationUsingAmbientTransaction(Specification specification);
        Task<IEnumerable<ReleasedProvider>> CreateReleasedProvidersUsingAmbientTransaction(IEnumerable<ReleasedProvider> releasedProviders);
        Task<bool> UpdateSpecificationUsingAmbientTransaction(Specification specification);
        Task<int> QueryPublishedFundingCount(
                    int channelId,
                    IEnumerable<string> fundingStreamIds,
                    IEnumerable<string> fundingPeriodIds,
                    IEnumerable<string> groupingReasons,
                    IEnumerable<string> variationReasons);
        Task<IEnumerable<ExternalFeedFundingGroupItem>> QueryPublishedFunding(
                    int channelId,
                    IEnumerable<string> fundingStreamIds,
                    IEnumerable<string> fundingPeriodIds,
                    IEnumerable<string> groupingReasons,
                    IEnumerable<string> variationReasons,
                    int top,
                    int? pageRef,
                    int totalCount);
        Task<bool> ContainsFundingId(int? channelId, string fundingId);
        Task<bool> ContainsProviderVersion(int channelId, string providerFundingVersion);
        Task<IEnumerable<string>> GetFundingGroupIdsForProviderFunding(int channelId, string publishedProviderVersion);
        Task<Specification> GetSpecificationById(string id);
        Task<FundingPeriod> CreateFundingPeriodUsingAmbientTransaction(FundingPeriod fundingPeriod);
        Task<FundingStream> CreateFundingStreamUsingAmbientTransaction(FundingStream fundingStream);
        Task<IEnumerable<ReleasedProviderChannelVariationReason>> CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(IEnumerable<ReleasedProviderChannelVariationReason> variationReason);
        Task<FundingGroupProvider> CreateFundingGroupProviderUsingAmbientTransaction(FundingGroupProvider fundingGroupProvider);
        Task<FundingGroupVersion> CreateFundingGroupVersionUsingAmbientTransaction(FundingGroupVersion fundingGroupVersion);
        Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReasonUsingAmbientTransaction(FundingGroupVersionVariationReason reason);
        Task<IEnumerable<ReleasedDataAllocationHistory>> GetPublishedProviderTransactionHistory(string specificationId, string providerId);
        Task<ReleasedProvider> CreateReleasedProvider(ReleasedProvider releasedProvider);
        Task<ReleasedProviderVersion> CreateReleasedProviderVersion(ReleasedProviderVersion releasedProviderVersion);
        Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionChannel(Guid fundingGroupId, int channelId, ISqlTransaction transaction = null);

        /// <summary>
        /// Get all released providers
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<ReleasedProvider>> GetReleasedProviders();

        /// <summary>
        /// Get all released providers for a given specification
        /// </summary>
        /// <param name="specificationId">Specification ID</param>
        /// <returns></returns>
        Task<IEnumerable<ReleasedProvider>> GetReleasedProviders(string specificationId);

        /// <summary>
        /// Get released providers for a specification and a set of provider IDs
        /// </summary>
        /// <param name="specificationId">Specification ID</param>
        /// <param name="providerIds">List of provider IDs</param>
        /// <returns></returns>
        Task<IEnumerable<ReleasedProvider>> GetReleasedProviders(string specificationId, IEnumerable<string> providerIds);
        Task<IEnumerable<ReleasedProviderVersion>> GetReleasedProviderVersions();
        Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersions();
        Task<ReleasedProviderChannelVariationReason> CreateReleasedProviderChannelVariationReason(ReleasedProviderChannelVariationReason reason);
        Task<ReleasedProviderVersionChannel> GetReleasedProviderVersionChannel(int releasedProviderVersionId, int channelId);
        Task<ProviderVersionInChannel> GetReleasedProvider(string publishedProviderVersion, int channelId);
        Task<IEnumerable<FundingGroup>> GetFundingGroups();
        Task<FundingGroupProvider> CreateFundingGroupProvider(FundingGroupProvider fundingGroupProvider);
        Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannel(ReleasedProviderVersionChannel providerVersionChannel);
        Task ClearDatabase();
        Task<bool> DatabaseHasExistingFundingData(IEnumerable<string> fundingStreamIds);
        Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionsBySpecificationId(string specificationId);
        Task<IEnumerable<FundingGroupVersion>> GetLatestFundingGroupVersionsBySpecificationId(string specificationId);
        Task<IEnumerable<LatestFundingGroupVersion>> GetLatestFundingGroupMajorVersionsBySpecificationId(string specificationId, int channelId);
        Task<IEnumerable<ReleasedProviderSummary>> GetReleasedProviderSummaryBySpecificationId(string specificationId);
        Task<IEnumerable<ReleasedProviderSummary>> GetLatestReleasedProviderSummaryBySpecificationId(string specificationId);
        Task<FundingGroup> CreateFundingGroupUsingAmbientTransaction(FundingGroup fundingGroup);
        Task<IEnumerable<FundingGroup>> BulkCreateFundingGroupsUsingAmbientTransaction(IEnumerable<FundingGroup> fundingGroups);
        Task<IEnumerable<FundingGroup>> GetFundingGroupsBySpecificationAndChannelUsingAmbientTransaction(string specificationId, int channelId);
        Task<IEnumerable<FundingGroupVersion>> BulkCreateFundingGroupVersionsUsingAmbientTransaction(IEnumerable<FundingGroupVersion> fundingGroupVersions);
        Task<IEnumerable<FundingGroupVersionVariationReason>> BulkCreateFundingGroupVersionVariationReasonsUsingAmbientTransaction(IEnumerable<FundingGroupVersionVariationReason> variationReasons);
        Task<IEnumerable<FundingGroupProvider>> BulkCreateFundingGroupProvidersUsingAmbientTransaction(IEnumerable<FundingGroupProvider> providers);
        Task<IEnumerable<ReleasedProvider>> BulkCreateReleasedProvidersUsingAmbientTransaction(IEnumerable<ReleasedProvider> releasedProviders);
        Task<IEnumerable<ReleasedProviderVersion>> BulkCreateReleasedProviderVersionsUsingAmbientTransaction(IEnumerable<ReleasedProviderVersion> releasedProviderVersions);
        Task<IEnumerable<ReleasedProviderChannelVariationReason>> BulkCreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(IEnumerable<ReleasedProviderChannelVariationReason> variationReasons);
        Task<IEnumerable<ReleasedProviderVersionChannel>> BulkCreateReleasedProviderVersionChannelsUsingAmbientTransaction(IEnumerable<ReleasedProviderVersionChannel> releasedProviderVersionChannels);
        Task<IEnumerable<ReleasedProviderVersionChannel>> GetLatestReleasedProviderVersionsId(string specificationId, string providerIds, int channelId, ISqlTransaction transaction = null);
        Task<IEnumerable<ReleasedProviderVersionChannelResult>> GetLatestReleasedProviderVersionsId(string specificationId, int channelId, ISqlTransaction transaction = null);
        Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersionsByChannelId(string specificationId, IEnumerable<int> channelIds, ISqlTransaction transaction = null);

        Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersionsByChannelIdUsingAmbientTransaction(string specificationId, IEnumerable<int> channelIds);
        Task<ReleasedProvider> CheckIsExistingReleaseProviderId(string providerId, string specificationId);
    }
}