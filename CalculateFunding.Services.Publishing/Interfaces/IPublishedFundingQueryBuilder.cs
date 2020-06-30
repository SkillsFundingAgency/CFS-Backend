using System.Collections.Generic;
using CalculateFunding.Common.CosmosDb;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingQueryBuilder
    {
        CosmosDbQuery BuildQuery(
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            int top,
            int? pageRef);

        CosmosDbQuery BuildCountQuery(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons);
    }
}