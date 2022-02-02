using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseManagementRepository
    {
        void InitialiseTransaction();

        void Commit();

        Task<SqlModels.GroupingReason> CreateGroupingReason(SqlModels.GroupingReason groupingReason);

        Task<IEnumerable<SqlModels.GroupingReason>> GetGroupingReasons();

        Task<IEnumerable<VariationReason>> GetVariationReasons();

        Task<VariationReason> CreateVariationReason(VariationReason reason);

        Task<IEnumerable<Channel>> GetChannels();
        Task<Channel> GetChannelByChannelCode(string channelCode);
        Task<int?> GetChannelIdFromUrlKey(string normalisedKey);
        Task<Channel> CreateChannel(Channel channel);
        Task<bool> UpdateChannel(Channel channel);
        Task<IEnumerable<FundingPeriod>> GetFundingPeriods();
        Task<IEnumerable<FundingStream>> GetFundingStreams();
        Task<FundingPeriod> GetFundingPeriodByCode(string code);
        Task<FundingStream> GetFundingStreamByCode(string code);
        Task<FundingStream> CreateFundingStream(FundingStream fundingStream);
        Task<FundingPeriod> CreateFundingPeriod(FundingPeriod fundingPeriod);
        Task<FundingGroup> CreateFundingGroup(FundingGroup fundingGroup);
        Task<FundingGroup> GetFundingGroup(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue);
        Task<ReleasedProviderVersion> CreateReleasedProviderVersionsUsingAmbientTransaction(ReleasedProviderVersion providerVersion);
        Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannelsUsingAmbientTransaction(ReleasedProviderVersionChannel providerVersionChannel);
        Task<IEnumerable<LatestProviderVersionInFundingGroup>> GetLatestProviderVersionInFundingGroups(string id, int channelId);
        Task<FundingGroupVersion> GetFundingGroupVersion(int fundingGroupId, int majorVersion);
        Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersions(string specificationId, IEnumerable<int> channelIds);
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
        Task<IEnumerable<ReleasedProvider>> GetReleasedProviders();
        Task<IEnumerable<ReleasedProviderVersion>> GetReleasedProviderVersions();
        Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersions();
        Task<ReleasedProviderChannelVariationReason> CreateReleasedProviderChannelVariationReason(ReleasedProviderChannelVariationReason reason);
        Task<ReleasedProviderVersionChannel> GetReleasedProviderVersionChannel(int releasedProviderVersionId, int channelId);
        Task<IEnumerable<FundingGroup>> GetFundingGroups();
        Task<FundingGroupProvider> CreateFundingGroupProvider(FundingGroupProvider fundingGroupProvider);
        Task<ReleasedProviderVersionChannel> CreateReleasedProviderVersionChannel(ReleasedProviderVersionChannel providerVersionChannel);
        Task ClearDatabase();
        Task<IEnumerable<FundingGroupVersion>> GetFundingGroupVersionsBySpecificationId(string specificationId);
        Task<IEnumerable<FundingGroupVersion>> GetLatestFundingGroupVersionsBySpecificationId(string specificationId);
        Task<IEnumerable<ReleasedProviderSummary>> GetReleasedProviderSummaryBySpecificationId(string specificationId);
        Task<IEnumerable<ReleasedProviderSummary>> GetLatestReleasedProviderSummaryBySpecificationId(string specificationId);
    }
}