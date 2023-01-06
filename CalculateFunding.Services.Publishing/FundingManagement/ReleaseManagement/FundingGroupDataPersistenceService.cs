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
    public class FundingGroupDataPersistenceService : IFundingGroupDataPersistenceService
    {
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly IUniqueIdentifierProvider _fundingGroupIdentifierGenerator;
        private readonly IUniqueIdentifierProvider _fundingGroupVariationReasonIdentifierGenerator;

        public FundingGroupDataPersistenceService(
            IReleaseManagementRepository releaseManagementRepository,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
            IUniqueIdentifierProvider fundingGroupIdentifierGenerator,
            IUniqueIdentifierProvider fundingGroupVariationReasonIdentifierGenerator)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(fundingGroupIdentifierGenerator, nameof(fundingGroupIdentifierGenerator));
            Guard.ArgumentNotNull(fundingGroupVariationReasonIdentifierGenerator, nameof(fundingGroupVariationReasonIdentifierGenerator));

            _releaseManagementRepository = releaseManagementRepository;
            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _fundingGroupIdentifierGenerator = fundingGroupIdentifierGenerator;
            _fundingGroupVariationReasonIdentifierGenerator = fundingGroupVariationReasonIdentifierGenerator;
        }

        public async Task<IEnumerable<FundingGroupVersion>> ReleaseFundingGroupData(IEnumerable<GeneratedPublishedFunding> fundingGroupData, int channelId)
        {
            Dictionary<string, SqlModels.GroupingReason> groupingReasons = (await _releaseManagementRepository.GetGroupingReasonsUsingAmbientTransaction()).ToDictionary(_ => _.GroupingReasonCode);
            Dictionary<string, FundingStream> fundingStreams = (await _releaseManagementRepository.GetFundingStreamsUsingAmbientTransaction()).ToDictionary(_ => _.FundingStreamCode);
            Dictionary<string, FundingPeriod> fundingPeriods = (await _releaseManagementRepository.GetFundingPeriodsUsingAmbientTransaction()).ToDictionary(_ => _.FundingPeriodCode);
            Dictionary<string, SqlModels.VariationReason> variationReasons = (await _releaseManagementRepository.GetVariationReasonsUsingAmbientTransaction()).ToDictionary(_ => _.VariationReasonCode);

            List<FundingGroupVersion> fundingGroupVersions = new List<FundingGroupVersion>();
            List<FundingGroupVersionVariationReason> createVariationReasons = new List<FundingGroupVersionVariationReason>();
            List<Guid> fundingGroupIds = new List<Guid>();
            #region Get all the FGV data from DB to get the channelVersion and to avoid multiple DB call
            foreach (GeneratedPublishedFunding fundingGroupDataItem in fundingGroupData)
            {
                if (_releaseToChannelSqlMappingContext.FundingGroups[channelId].TryGetValue(fundingGroupDataItem.OrganisationGroupResult, out Guid fundingGroupId))
                { 
                    fundingGroupIds.Add(fundingGroupId);
                }
            }
            IEnumerable<FundingGroupVersion> fundingGroupVersionsFromDb = await _releaseManagementRepository.GetFundingGroupVersionChannelForAllFundingId(fundingGroupIds, channelId);
            Dictionary<Guid, FundingGroupVersion> fundingGroupVersionDict = fundingGroupVersionsFromDb?.ToDictionary(_=>_.FundingGroupId, _=>_)
                ?? new Dictionary<Guid, FundingGroupVersion>();
            #endregion
            foreach (GeneratedPublishedFunding fundingGroupDataItem in fundingGroupData)
            {
                if (!_releaseToChannelSqlMappingContext.FundingGroups[channelId].TryGetValue(fundingGroupDataItem.OrganisationGroupResult, out Guid fundingGroupId))
                {
                    throw new KeyNotFoundException(
                        $"OrganisationGroupResult not found in sql context for published funding id = {fundingGroupDataItem.PublishedFundingVersion.FundingId}");
                }
                PublishedFundingVersion pfv = fundingGroupDataItem.PublishedFundingVersion;

                if (string.IsNullOrWhiteSpace(pfv.FundingId))
                {
                    throw new InvalidOperationException("Funding ID is null or empty");
                }
                fundingGroupVersionDict.TryGetValue(fundingGroupId, out FundingGroupVersion fgvFromDb);
                FundingGroupVersion fundingGroupVersion = new FundingGroupVersion
                {
                    FundingGroupVersionId = _fundingGroupIdentifierGenerator.GenerateIdentifier(),
                    FundingGroupId = fundingGroupId,
                    ChannelId = channelId,
                    GroupingReasonId = groupingReasons[pfv.GroupingReason.ToString()].GroupingReasonId,
                    StatusChangedDate = pfv.StatusChangedDate,
                    MajorVersion = pfv.MajorVersion,
                    MinorVersion = pfv.MinorVersion,
                    TemplateVersion = pfv.TemplateVersion,
                    SchemaVersion = pfv.SchemaVersion,
                    JobId = pfv.JobId,
                    CorrelationId = pfv.CorrelationId,
                    FundingStreamId = fundingStreams[pfv.FundingStreamId].FundingStreamId,
                    FundingPeriodId = fundingPeriods[pfv.FundingPeriod.Id].FundingPeriodId,
                    FundingId = pfv.FundingId,
                    TotalFunding = pfv.TotalFunding ?? 0m,
                    ExternalPublicationDate = pfv.ExternalPublicationDate,
                    EarliestPaymentAvailableDate = pfv.EarliestPaymentAvailableDate,
                    ChannelVersion = (fgvFromDb != null) ? (fgvFromDb.ChannelVersion + 1) : await GetFundingGroupChannelVersion(fundingGroupId, channelId)
                };

                createVariationReasons.AddRange(pfv.VariationReasons.Select(variationReason =>
                    new FundingGroupVersionVariationReason
                    {
                        FundingGroupVersionVariationReasonId = _fundingGroupVariationReasonIdentifierGenerator.GenerateIdentifier(),
                        FundingGroupVersionId = fundingGroupVersion.FundingGroupVersionId,
                        VariationReasonId = variationReasons[variationReason.ToString()].VariationReasonId
                    }));

                if (!_releaseToChannelSqlMappingContext.FundingGroupVersions.TryGetValue(channelId, out Dictionary<string, FundingGroupVersion> fundingGroupVersionsForChannel))
                {
                    fundingGroupVersionsForChannel = new Dictionary<string, FundingGroupVersion>();
                    _releaseToChannelSqlMappingContext.FundingGroupVersions.Add(channelId, fundingGroupVersionsForChannel);
                }
                
                fundingGroupVersionsForChannel.Add(pfv.FundingId, fundingGroupVersion);
                fundingGroupVersions.Add(fundingGroupVersion);
            }

            if (fundingGroupVersions.Any())
            {
                await _releaseManagementRepository.BulkCreateFundingGroupVersionsUsingAmbientTransaction(
                    fundingGroupVersions);
            }

            if (createVariationReasons.Any())
            {
                await _releaseManagementRepository.BulkCreateFundingGroupVersionVariationReasonsUsingAmbientTransaction(
                    createVariationReasons);
            }

            return fundingGroupVersions;
        }
        private async Task<int> GetFundingGroupChannelVersion(Guid fundingGroupId, int channelId)
        {
            var channelVersionEntry = await _releaseManagementRepository.GetFundingGroupVersionChannel(fundingGroupId, channelId);
            int channelVersion = (channelVersionEntry != null && channelVersionEntry.Count() > 0) ? channelVersionEntry.FirstOrDefault().ChannelVersion : 0;
            return (channelVersion + 1);
        }
    }
}
