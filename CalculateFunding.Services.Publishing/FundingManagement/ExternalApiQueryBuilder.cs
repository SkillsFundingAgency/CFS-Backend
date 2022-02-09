using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class ExternalApiQueryBuilder : IExternalApiQueryBuilder
    {
        public (string sql, object parameters) BuildCountQuery(
           int channelId,
           IEnumerable<string> fundingStreamIds,
           IEnumerable<string> fundingPeriodIds,
           IEnumerable<string> groupingReasons,
           IEnumerable<string> variationReasons)
        {
            return (
             $@"
                SELECT COUNT(*)
                {BuildFromClauseAndPredicates(fundingStreamIds, fundingPeriodIds, groupingReasons, variationReasons)}"
                ,
                new
                {
                    channelId,
                    fundingStreamIds,
                    fundingPeriodIds,
                    groupingReasons,
                    variationReasons
                });

        }

        public (string sql, object parameters) BuildQuery(
            int channelId,
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            int top,
            int? pageRef,
            int totalCount)
        {
            int startAt = GetStartAt(pageRef, top, totalCount);

            return (

                $@"
                SELECT FGV.FundingId, FGV.MajorVersion, FGV.StatusChangedDate
                
                {BuildFromClauseAndPredicates(fundingStreamIds, fundingPeriodIds, groupingReasons, variationReasons)}
                ORDER BY FGV.StatusChangedDate ASC, FGV.FundingGroupVersionId ASC
                OFFSET @startAt ROWS FETCH NEXT @pageSize ROWS ONLY",

        new
        {
            channelId,
            fundingStreamIds,
            fundingPeriodIds,
            groupingReasons,
            variationReasons,
            startAt = startAt,
            pageSize = top
        });
        }

        private string BuildFromClauseAndPredicates(
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons)
        {
            return $@"FROM FundingGroupVersions FGV
                {FundingStreamsJoin(fundingStreamIds)}
                {FundingPeriodsJoin(fundingPeriodIds)}
                {GroupingReasonsJoin(groupingReasons)}
                {FundingGroupVariationReasonsJoin(variationReasons)}
                {VariationReasonsJoin(variationReasons)}
                WHERE FGV.ChannelId = @channelId
                {FundingStreamsPredicate(fundingStreamIds)}
                {FundingPeriodsPredicate(fundingPeriodIds)}
                {GroupingReasonsPredicate(groupingReasons)}
                {VariationReasonsPredicate(variationReasons)}";
        }

        private string FundingStreamsPredicate(IEnumerable<string> fundingStreamIds)
            => InPredicateFor(fundingStreamIds, "FS.FundingStreamCode", nameof(fundingStreamIds));

        private string FundingStreamsJoin(IEnumerable<string> fundingStreamIds)
            => JoinQueryFor(fundingStreamIds, "FundingStreamId", "FundingStreams", "FS");

        private string FundingPeriodsPredicate(IEnumerable<string> fundingPeriodIds)
            => InPredicateFor(fundingPeriodIds, "FP.FundingPeriodCode", nameof(fundingPeriodIds));

        private string FundingPeriodsJoin(IEnumerable<string> fundingPeriodIds)
            => JoinQueryFor(fundingPeriodIds, "FundingPeriodId", "FundingPeriods", "FP");

        private string GroupingReasonsPredicate(IEnumerable<string> groupingReasons)
            => InPredicateFor(groupingReasons, "GR.GroupingReasonCode", nameof(groupingReasons));

        private string GroupingReasonsJoin(IEnumerable<string> groupingReasons)
            => JoinQueryFor(groupingReasons, "GroupingReasonId", "GroupingReasons", "GR");

        private string VariationReasonsPredicate(IEnumerable<string> variationReasons)
            => InPredicateFor(variationReasons, "VR.VariationReasonCode", nameof(variationReasons));

        private string FundingGroupVariationReasonsJoin(IEnumerable<string> variationReasons)
           => JoinQueryFor(variationReasons, "FundingGroupVersionId", "FundingGroupVersionVariationReasons", "FGVVR");

        private string VariationReasonsJoin(IEnumerable<string> variationReasons)
            => JoinQueryFor(variationReasons, "VariationReasonId", "VariationReasons", "VR", "FGVVR");

        private string InPredicateFor(IEnumerable<string> matches, string field, string parameterFieldName)
        {
            return !matches.IsNullOrEmpty() ?
                $"AND {field} IN @{parameterFieldName}" :
                null;
        }

        private string JoinQueryFor(IEnumerable<string> matches, string joinFieldName, string tableName, string tableAlias, string sourceTableFieldAlias = "FGV")
        {
            return !matches.IsNullOrEmpty() ?
                $"INNER JOIN {tableName} AS {tableAlias} ON {tableAlias}.{joinFieldName} = {sourceTableFieldAlias}.{joinFieldName}" :
                null;
        }

        private int GetStartAt(int? pageRef, int top, int totalCount)
        {
            int offset = totalCount % top == 0 ? top : totalCount % top;

            return pageRef.HasValue ?
                (pageRef.Value - 1) * top: 
                Math.Max(0, totalCount - offset);
        }
    }
}
