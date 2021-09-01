using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public interface IExternalApiQueryBuilder
    {
        (string sql, object parameters) BuildCountQuery(int channelId, IEnumerable<string> fundingStreamIds, IEnumerable<string> fundingPeriodIds, IEnumerable<string> groupingReasons, IEnumerable<string> variationReasons);
        (string sql, object parameters) BuildQuery(int channelId, IEnumerable<string> fundingStreamIds, IEnumerable<string> fundingPeriodIds, IEnumerable<string> groupingReasons, IEnumerable<string> variationReasons, int top, int? pageRef, int totalCount);
    }
}