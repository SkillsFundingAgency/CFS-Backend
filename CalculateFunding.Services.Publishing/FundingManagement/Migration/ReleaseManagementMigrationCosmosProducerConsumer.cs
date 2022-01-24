using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Migration
{
    public class ReleaseManagementMigrationCosmosProducerConsumer<T> : IReleaseManagementMigrationCosmosProducerConsumer<T> where T : IIdentifiable
    {
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly ILogger _logger;

        public ReleaseManagementMigrationCosmosProducerConsumer(IProducerConsumerFactory producerConsumerFactory, ILogger logger)
        {
            _producerConsumerFactory = producerConsumerFactory;
            _logger = logger;
        }

        public async Task RunAsync(Dictionary<string, FundingStream> fundingStreams,
            Dictionary<string, FundingPeriod> fundingPeriods, Dictionary<string, Channel> channels,
            Dictionary<string, SqlModels.GroupingReason> groupingReasons, Dictionary<string, VariationReason> variationReasons, 
            Dictionary<string, Specification> specifications, Dictionary<string, ReleasedProvider> releasedProviders,
            Dictionary<string, ReleasedProviderVersion> releasedProviderVersions,
            ICosmosDbFeedIterator cosmosDbFeedIterator,
            Func<CancellationToken, dynamic, ArraySegment<T>, Task> consumer)
        {
            IReleaseManagementImportContext importContext = new ReleaseManagementImportContext()
            {
                Documents = cosmosDbFeedIterator,
                FundingStreams = fundingStreams,
                FundingPeriods = fundingPeriods,
                Channels = channels,
                GroupingReasons = groupingReasons,
                VariationReasons = variationReasons,
                Specifications = specifications,
                ReleasedProviders = releasedProviders,
                ReleasedProviderVersion = releasedProviderVersions,
                JobId = Guid.NewGuid().ToString(),
            };

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(Producer,
               consumer,
               10,
               1,
               _logger);

            await producerConsumer.Run(importContext);
        }

        private async Task<(bool Complete, ArraySegment<T> Item)> Producer(CancellationToken cancellationToken, dynamic context)
        {
            try
            {
                ICosmosDbFeedIterator feed = ((IReleaseManagementImportContext)context).Documents;

                if (!feed.HasMoreResults)
                {
                    return (true, ArraySegment<T>.Empty);
                }

                IEnumerable<T> documents = await feed.ReadNext<T>(cancellationToken);

                while (documents.IsNullOrEmpty() && feed.HasMoreResults)
                {
                    documents = await feed.ReadNext<T>(cancellationToken);
                }

                if (documents.IsNullOrEmpty() && !feed.HasMoreResults)
                {
                    return (true, ArraySegment<T>.Empty);
                }

                return (false, documents.ToArray());
            }
            catch
            {
                return (true, ArraySegment<T>.Empty);
            };
        }
    }
}
