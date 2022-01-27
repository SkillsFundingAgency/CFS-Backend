using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseManagementMigrationCosmosProducerConsumer<T> where T : IIdentifiable
    {
        Task RunAsync(Dictionary<string, FundingStream> fundingStreams,
            Dictionary<string, FundingPeriod> fundingPeriods,
            Dictionary<string, Channel> channels,
            Dictionary<string, SqlModels.GroupingReason> groupingReasons,
            Dictionary<string, VariationReason> variationReasons,
            Dictionary<string, Specification> specifications,
            ICosmosDbFeedIterator cosmosDbFeedIterator,
            Func<CancellationToken, dynamic, ArraySegment<T>, Task> consumer);
    }
}
