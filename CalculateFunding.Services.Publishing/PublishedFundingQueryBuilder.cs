using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingQueryBuilder : IPublishedFundingQueryBuilder
    {
        public CosmosDbQuery BuildCountQuery(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons)
        {
            return new CosmosDbQuery
            {
                QueryText = $@"
                SELECT
                   VALUE COUNT(1)
                {BuildFromClauseAndPredicates(fundingStreamIds, fundingPeriodIds, groupingReasons, variationReasons)}"
            };
        }

        public CosmosDbQuery BuildQuery(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            int top,
            int? pageRef,
            int totalCount)
        {
            return new CosmosDbQuery
            {
                QueryText = $@"
                SELECT
                    p.content.fundingId AS id,
                    p.content.statusChangedDate,
                    p.content.fundingStreamId,
                    p.content.fundingPeriod.id AS FundingPeriodId,
                    p.content.groupingReason AS GroupingType,
                    p.content.organisationGroupTypeCode AS GroupTypeIdentifier,
                    p.content.organisationGroupIdentifierValue AS IdentifierValue,
                    p.content.version,
                    CONCAT(p.content.fundingStreamId, '-', 
                            p.content.fundingPeriod.id, '-',
                            p.content.groupingReason, '-',
                            p.content.organisationGroupTypeCode, '-',
                            ToString(p.content.organisationGroupIdentifierValue), '-',
                            ToString(p.content.majorVersion), '_',
                            ToString(p.content.minorVersion), '.json')
                    AS DocumentPath,
                    p.deleted
                {BuildFromClauseAndPredicates(fundingStreamIds, fundingPeriodIds, groupingReasons, variationReasons)}
                ORDER BY p.documentType,
				p.content.statusChangedDate, 
				p.content.id,
				p.content.fundingStreamId,
				p.content.fundingPeriod.id,
				p.content.groupingReason,
				p.deleted
                {PagingSkipLimit(pageRef, top, totalCount)}"
            };
        }

        private string BuildFromClauseAndPredicates(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons)
        {
            return $@"FROM publishedFunding p
                {VariationReasonsJoin(variationReasons)}
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                {FundingStreamsPredicate(fundingStreamIds)}
                {FundingPeriodsPredicate(fundingPeriodIds)}
                {GroupingReasonsPredicate(groupingReasons)}
                {VariationReasonsPredicate(variationReasons)}";
        }

        private string FundingStreamsPredicate(IEnumerable<string> fundingStreamIds)
            => InPredicateFor(fundingStreamIds, "p.content.fundingStreamId");

        private string FundingPeriodsPredicate(IEnumerable<string> fundingPeriodIds)
            => InPredicateFor(fundingPeriodIds, "p.content.fundingPeriod.id");

        private string GroupingReasonsPredicate(IEnumerable<string> groupingReasons)
            => InPredicateFor(groupingReasons, "p.content.groupingReason");

        private string VariationReasonsPredicate(IEnumerable<string> variationReasons)
            => InPredicateFor(variationReasons, "variationReasons");

        private string VariationReasonsJoin(IEnumerable<string> variationReasons)
            => JoinQueryFor(variationReasons, "p.content.variationReasons", "variationReasons");

        private string InPredicateFor(IEnumerable<string> matches, string field)
        {
            return !matches.IsNullOrEmpty() ?
                $"AND {field} IN ({string.Join(",", matches.Select(_ => $"'{_}'"))})" :
                null;
        }

        private string JoinQueryFor(IEnumerable<string> matches, string field, string fieldCode)
        {
            return !matches.IsNullOrEmpty() ?
                $"JOIN {fieldCode} IN {field}" :
                null;
        }

        private string PagingSkipLimit(int? pageRef, int top, int totalCount)
        {
            return pageRef.HasValue ?
                $"OFFSET {(pageRef - 1) * top} LIMIT {top}" :
                $"OFFSET {Math.Max(0, totalCount - top)} LIMIT {top}";
        }
    }
}