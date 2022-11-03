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
    public abstract class PublishedFundingVersionsUndoTaskBase : UndoTaskBase, IPublishedFundingUndoJobTask
    {
        protected PublishedFundingVersionsUndoTaskBase(IPublishedFundingUndoCosmosRepository cosmos, 
            IPublishedFundingUndoBlobStoreRepository blobStore, 
            IProducerConsumerFactory producerConsumerFactory, 
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
        }
        public bool VersionDocuments => true;

        public async Task Run(PublishedFundingUndoTaskContext taskContext)
        {
            LogStartingTask();
            
            Guard.ArgumentNotNull(taskContext?.UndoTaskDetails, nameof(taskContext.UndoTaskDetails));

            ICosmosDbFeedIterator feed = GetPublishedFundingVersionsFeed(taskContext.UndoTaskDetails);

            FeedContext feedContext = new FeedContext(taskContext, feed);

            IProducerConsumer producerConsumer = ProducerConsumerFactory.CreateProducerConsumer(ProducePublishedFundingVersions,
                UndoPublishedFundingVersions,
                200,
                4,
                Logger);

            await producerConsumer.Run(feedContext);

            await NotifyJobProgress(taskContext);
            
            LogCompletedTask();
        }

        protected virtual ICosmosDbFeedIterator GetPublishedFundingVersionsFeed(UndoTaskDetails details) =>
            Cosmos.GetPublishedFundingVersions(details.FundingStreamId,
                details.FundingPeriodId,
                details.TimeStamp);

        private async Task<(bool isComplete, IEnumerable<PublishedFundingVersion> items)> ProducePublishedFundingVersions(CancellationToken cancellationToken,
            dynamic context)
        {
            return await GetDocumentsFromFeed<PublishedFundingVersion>(cancellationToken, context);
        }

        protected abstract Task UndoPublishedFundingVersions(CancellationToken cancellationToken, 
            dynamic context, 
            IEnumerable<PublishedFundingVersion> publishedFundingVersions);

        protected async Task DeleteBlobDocuments(IEnumerable<PublishedFundingVersion> publishedFundingVersions, string apiVersion, string channelCodes)
        {
            LogInformation($"Deleting {publishedFundingVersions.Count()} published funding version blobs");
            
            foreach (PublishedFundingVersion publishedProviderVersion in publishedFundingVersions.Where(_ => _.MajorVersion > 0 && _.MinorVersion == 0))
            {
                //if api version is 4, delete the channel specific blobs
                if ((!string.IsNullOrEmpty(apiVersion)) && apiVersion.Equals(PublishedFundingUndoJobParameters.APIVersion_4))
                {
                    IEnumerable<string> eligibleChannels = channelCodes.Split(',').ToList();
                    eligibleChannels.ForEach(channelCode =>
                    {
                        if (!string.IsNullOrWhiteSpace(channelCode))
                        {
                            BlobStore.RemoveReleasedGroupBlob(publishedProviderVersion, channelCode);
                        }
                    });
                }
                else
                {
                    await BlobStore.RemovePublishedFundingVersionBlob(publishedProviderVersion);
                }
            }
        }
        protected async Task DeleteBlobDocuments(IEnumerable<PublishedFundingVersion> publishedFundingVersions)
        {
            LogInformation($"Deleting {publishedFundingVersions.Count()} published funding version blobs");

            foreach (PublishedFundingVersion publishedProviderVersion in publishedFundingVersions.Where(_ => _.MajorVersion > 0 && _.MinorVersion == 0))
            {
                await BlobStore.RemovePublishedFundingVersionBlob(publishedProviderVersion);
            }
        }
    }
}