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
            IEnumerable<string> groupingReasons)
        {
            return new CosmosDbQuery
            {
                QueryText = $@"
                SELECT
                   VALUE COUNT(1)
                {BuildFromClauseAndPredicates(fundingStreamIds, fundingPeriodIds, groupingReasons)}"
            };
        }
        
        public CosmosDbQuery BuildQuery(
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            int top,
            int? pageRef)
        {
            return new CosmosDbQuery
            {
                QueryText = $@"
                SELECT
                    p.content.id,
                    p.content.current.statusChangedDate,
                    p.content.current.fundingStreamId,
                    p.content.current.fundingPeriod.id AS FundingPeriodId,
                    p.content.current.groupingReason AS GroupingType,
                    p.content.current.organisationGroupTypeCode AS GroupTypeIdentifier,
                    p.content.current.organisationGroupIdentifierValue AS IdentifierValue,
                    p.content.current.version,
                    CONCAT(p.content.current.fundingStreamId, '-', 
                            p.content.current.fundingPeriod.id, '-',
                            p.content.current.groupingReason, '-',
                            p.content.current.organisationGroupTypeCode, '-',
                            ToString(p.content.current.organisationGroupIdentifierValue), '-',
                            ToString(p.content.current.majorVersion), '_',
                            ToString(p.content.current.minorVersion), '.json')
                    AS DocumentPath,
                    p.deleted
                {BuildFromClauseAndPredicates(fundingStreamIds, fundingPeriodIds, groupingReasons)}
                ORDER BY p.documentType,
				p.content.current.statusChangedDate, 
				p.content.id,
				p.content.current.fundingStreamId,
				p.content.current.fundingPeriod.id,
				p.content.current.groupingReason,
				p.deleted
                {PagingSkipLimit(pageRef, top)}"
            };
        }

        private string BuildFromClauseAndPredicates(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons)
        {
            return $@"FROM publishedFunding p
                WHERE p.documentType = 'PublishedFunding'
                AND p.deleted = false
                {FundingStreamsPredicate(fundingStreamIds)}
                {FundingPeriodsPredicate(fundingPeriodIds)}
                {GroupingReasonsPredicate(groupingReasons)}";
        }

        private string FundingStreamsPredicate(IEnumerable<string> fundingStreamIds)
            => InPredicateFor(fundingStreamIds, "p.content.current.fundingStreamId");

        private string FundingPeriodsPredicate(IEnumerable<string> fundingPeriodIds)
            => InPredicateFor(fundingPeriodIds, "p.content.current.fundingPeriod.id");

        private string GroupingReasonsPredicate(IEnumerable<string> groupingReasons) 
            => InPredicateFor(groupingReasons, "p.content.current.groupingReason");

        private string InPredicateFor(IEnumerable<string> matches, string field)
        {
            return !matches.IsNullOrEmpty() ? 
                $"AND {field} IN ({string.Join(",", matches.Select(_ => $"'{_}'"))})" :
                null;
        }

        private string PagingSkipLimit(int? pageRef, int top)
        {
            return pageRef.HasValue ? 
                $"OFFSET {(pageRef - 1) * top} LIMIT {top}" : 
                $"LIMIT {top}";
        }
    }
}