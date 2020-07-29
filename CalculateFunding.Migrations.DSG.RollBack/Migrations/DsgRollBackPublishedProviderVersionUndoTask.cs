using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using Serilog;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public class DsgRollBackPublishedProviderVersionUndoTask : PublishedProviderVersionsUndoTaskBase
    {
        public DsgRollBackPublishedProviderVersionUndoTask(IPublishedFundingUndoCosmosRepository cosmos,
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger,
            IJobTracker jobTracker) : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
        }

        protected override async Task UndoPublishedProviderVersions(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
            await DeleteDocuments(publishedProviderVersions, _ => _.PartitionKey, true);
        }

        protected override ICosmosDbFeedIterator<PublishedProviderVersion> GetPublishedProviderVersionsFeed(UndoTaskDetails details)
            => Cosmos.GetPublishedProviderVersionsFromVersion(details.FundingStreamId,
                details.FundingPeriodId,
                details.Version);
        
        protected override Task NotifyProgress(PublishedFundingUndoTaskContext taskContext) => Task.CompletedTask;
    }
}