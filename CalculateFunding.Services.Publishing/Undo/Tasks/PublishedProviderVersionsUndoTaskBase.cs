using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo.Tasks
{
    public abstract class PublishedProviderVersionsUndoTaskBase : UndoTaskBase, IPublishedFundingUndoJobTask
    {
        protected PublishedProviderVersionsUndoTaskBase(IPublishedFundingUndoCosmosRepository cosmos, 
            IPublishedFundingUndoBlobStoreRepository blobStore, 
            IProducerConsumerFactory producerConsumerFactory, 
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
        }

        public async Task Run(PublishedFundingUndoTaskContext taskContext)
        {
            LogStartingTask();
            
            Guard.ArgumentNotNull(taskContext?.PublishedProviderVersionDetails, nameof(taskContext.PublishedProviderVersionDetails));
            
            UndoTaskDetails details = taskContext.PublishedProviderVersionDetails;

            ICosmosDbFeedIterator<PublishedProviderVersion> feed = GetPublishedProviderVersionsFeed(details);

            FeedContext<PublishedProviderVersion> feedContext = new FeedContext<PublishedProviderVersion>(taskContext, feed);

            IProducerConsumer producerConsumer = ProducerConsumerFactory.CreateProducerConsumer(ProducePublishedProviderVersions,
                UndoPublishedProviderVersions,
                200,
                4,
                Logger);

            await producerConsumer.Run(feedContext);

            await NotifyJobProgress(taskContext);
            
            LogCompletedTask();
        }

        protected virtual ICosmosDbFeedIterator<PublishedProviderVersion> GetPublishedProviderVersionsFeed(UndoTaskDetails details) =>
            Cosmos.GetPublishedProviderVersions(details.FundingStreamId,
                details.FundingPeriodId,
                details.TimeStamp);

        private async Task<(bool isComplete, IEnumerable<PublishedProviderVersion> items)> ProducePublishedProviderVersions(CancellationToken cancellationToken,
            dynamic context)
        {
            return await GetDocumentsFromFeed<PublishedProviderVersion>(cancellationToken, context);
        }

        protected abstract Task UndoPublishedProviderVersions(CancellationToken cancellationToken, 
            dynamic context, 
            IEnumerable<PublishedProviderVersion> publishedProviderVersions);

        protected async Task DeleteBlobDocuments(IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
            LogInformation($"Requesting deletion of {publishedProviderVersions.Count()} published provider version blobs");
            
            foreach (PublishedProviderVersion publishedProviderVersion in publishedProviderVersions)
            {
                await BlobStore.RemovePublishedProviderVersionBlob(publishedProviderVersion);
            }
        }
    }
}