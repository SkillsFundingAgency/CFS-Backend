using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseManagementRepository
    {
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
        Task<FundingStream> CreateFundingStream(FundingStream fundingStream);
        Task<FundingPeriod> CreateFundingPeriod(FundingPeriod fundingPeriod);
        Task<FundingGroup> CreateFundingGroup(FundingGroup fundingGroup);
        Task<FundingGroup> GetFundingGroup(int channelId, string specificationId, int groupingReasonId, string organisationGroupTypeClassification, string organisationGroupIdentifierValue);
        Task<IEnumerable<LatestProviderVersionInFundingGroup>> GetLatestProviderVersionInFundingGroups(string id, int channelId);
        Task<FundingGroupVersion> GetFundingGroupVersion(int fundingGroupId, int majorVersion);
        Task<IEnumerable<ProviderVersionInChannel>> GetLatestPublishedProviderVersions(string specificationId, IEnumerable<int> channelIds);
        Task<FundingGroupVersion> CreateFundingGroupVersion(FundingGroupVersion fundingGroupVersion);
        Task<FundingGroupVersionVariationReason> CreateFundingGroupVariationReason(FundingGroupVersionVariationReason reason);
        Task<IEnumerable<Specification>> GetSpecifications();
        Task<Specification> CreateSpecification(Specification specification);
        Task<IEnumerable<ReleasedProvider>> CreateReleasedProviders(IEnumerable<ReleasedProvider> releasedProviders);

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
    }
}