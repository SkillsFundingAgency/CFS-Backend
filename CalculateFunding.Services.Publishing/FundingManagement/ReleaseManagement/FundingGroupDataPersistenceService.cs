using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
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

        public FundingGroupDataPersistenceService(
            IReleaseManagementRepository releaseManagementRepository,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));

            _releaseManagementRepository = releaseManagementRepository;
            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
        }

        public async Task ReleaseFundingGroupData(IEnumerable<(PublishedFundingVersion, OrganisationGroupResult)> fundingGroupData, int channelId)
        {
            IEnumerable<SqlModels.GroupingReason> groupingReasons = await _releaseManagementRepository.GetGroupingReasons();
            IEnumerable<FundingStream> fundingStreams = await _releaseManagementRepository.GetFundingStreams();
            IEnumerable<FundingPeriod> fundingPeriods = await _releaseManagementRepository.GetFundingPeriods();
            IEnumerable<SqlModels.VariationReason> variationReasons = await _releaseManagementRepository.GetVariationReasons();

            List<FundingGroupVersion> fundingGroupVersions = new List<FundingGroupVersion>();
            foreach ((PublishedFundingVersion, OrganisationGroupResult) fundingGroupDataItem in fundingGroupData)
            {
                if (!_releaseToChannelSqlMappingContext.FundingGroups.TryGetValue(fundingGroupDataItem.Item2, out int fundingGroupId))
                {
                    throw new KeyNotFoundException(
                        $"OrganisationGroupResult not found in sql context for published funding id = {fundingGroupDataItem.Item1.Id}");
                }

                PublishedFundingVersion pfv = fundingGroupDataItem.Item1;
                FundingGroupVersion fundingGroupVersion = new FundingGroupVersion
                {
                    FundingGroupId = fundingGroupId,
                    ChannelId = channelId,
                    GroupingReasonId = groupingReasons.Single(
                        _ => _.GroupingReasonCode == pfv.GroupingReason.ToString()).GroupingReasonId,
                    StatusChangedDate = pfv.StatusChangedDate,
                    MajorVersion = pfv.MajorVersion,
                    MinorVersion = pfv.MinorVersion,
                    TemplateVersion = pfv.TemplateVersion,
                    SchemaVersion = pfv.SchemaVersion,
                    JobId = pfv.JobId,
                    CorrelationId = pfv.CorrelationId,
                    FundingStreamId = fundingStreams.Single(
                        _ => _.FundingStreamCode == pfv.FundingStreamId).FundingStreamId,
                    FundingPeriodId = fundingPeriods.Single(
                        _ => _.FundingPeriodCode == pfv.FundingPeriod.Id).FundingPeriodId,
                    FundingId = pfv.FundingId,
                    TotalFunding = pfv.TotalFunding ?? 0m,
                    ExternalPublicationDate = pfv.ExternalPublicationDate,
                    EarliestPaymentAvailableDate = pfv.EarliestPaymentAvailableDate
                };

                FundingGroupVersion savedFundingGroupVersion = await _releaseManagementRepository.CreateFundingGroupVersionUsingAmbientTransaction(fundingGroupVersion);

                await PersistVariationReasons(variationReasons, pfv, savedFundingGroupVersion);

                foreach (string fundingId in pfv.ProviderFundings)
                {
                    string providerId = fundingId.Split("-")[2];
                    string key = $"{providerId}_{channelId}";
                    _releaseToChannelSqlMappingContext.FundingGroupVersions.TryAdd(key, savedFundingGroupVersion);
                }
            }
        }

        private async Task PersistVariationReasons(IEnumerable<SqlModels.VariationReason> variationReasons, PublishedFundingVersion pfv, FundingGroupVersion savedFundingGroupVersion)
        {
            foreach (CalculateFunding.Models.Publishing.VariationReason variationReason in pfv.VariationReasons)
            {
                FundingGroupVersionVariationReason fgvvr = new FundingGroupVersionVariationReason
                {
                    FundingGroupVersionId = savedFundingGroupVersion.FundingGroupVersionId,
                    FundingGroupVersionVariationReasonId = variationReasons.Single(_ => _.VariationReasonCode == variationReason.ToString()).VariationReasonId
                };

                await _releaseManagementRepository.CreateFundingGroupVariationReasonUsingAmbientTransaction(fgvvr);
            }
        }
    }
}
