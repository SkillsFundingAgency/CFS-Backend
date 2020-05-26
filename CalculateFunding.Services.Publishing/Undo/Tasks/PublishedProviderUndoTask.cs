using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo.Tasks
{
    public class PublishedProviderUndoTask : UndoTaskBase, IPublishedFundingUndoJobTask
    {
        public PublishedProviderUndoTask(IPublishedFundingUndoCosmosRepository cosmos,
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger,
            IJobTracker jobTracker,
            bool isHardDelete)
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
            IsHardDelete = isHardDelete;
        }

        public bool IsHardDelete { get; }

        public async Task Run(PublishedFundingUndoTaskContext taskContext)
        {
            LogStartingTask();
            
            Guard.ArgumentNotNull(taskContext?.PublishedProviderDetails, nameof(taskContext.PublishedProviderDetails));
            
            CorrelationIdDetails details = taskContext.PublishedProviderDetails;

            ICosmosDbFeedIterator<PublishedProvider> feed = Cosmos.GetPublishedProviders(details.FundingStreamId,
                details.FundingPeriodId,
                details.TimeStamp);

            FeedContext<PublishedProvider> feedContext = new FeedContext<PublishedProvider>(taskContext, feed);

            IProducerConsumer producerConsumer = ProducerConsumerFactory.CreateProducerConsumer(ProducePublishedProviders,
                UndoPublishedProviders,
                200,
                4,
                Logger);

            await producerConsumer.Run(feedContext);

            await NotifyJobProgress(taskContext);
            
            LogCompletedTask();
        }

        private async Task<(bool isComplete, IEnumerable<PublishedProvider> items)> ProducePublishedProviders(CancellationToken cancellationToken,
            dynamic context)
        {
            return await GetDocumentsFromFeed<PublishedProvider>(cancellationToken, context);
        }

        protected async Task UndoPublishedProviders(CancellationToken cancellationToken, 
            dynamic context, 
            IEnumerable<PublishedProvider> publishedProviders)
        {
            PublishedFundingUndoTaskContext taskContext = GetTaskContext(context);

            List<PublishedProvider> publishedProvidersToDelete = new List<PublishedProvider>();
            List<PublishedProvider> publishedProviderToUpdate = new List<PublishedProvider>();
            
            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                PublishedProviderVersion previousVersion = await GetPreviousPublishedProviderVersion(publishedProvider.Current.ProviderId, taskContext);

                if (previousVersion == null)
                {
                    publishedProvidersToDelete.Add(publishedProvider);
                }
                else
                {
                    publishedProvider.Current = previousVersion;
                    publishedProviderToUpdate.Add(publishedProvider);
                }
            }

            Task[] cosmosUpdates = new[]
            {
                DeleteDocuments(publishedProvidersToDelete, _ => _.PartitionKey, IsHardDelete),
                UpdateDocuments(publishedProviderToUpdate, _ => _.PartitionKey)
            };

            await TaskHelper.WhenAllAndThrow(cosmosUpdates);
        }

        protected async Task<PublishedProviderVersion> GetPreviousPublishedProviderVersion(string providerId, PublishedFundingUndoTaskContext taskContext)
        {
            CorrelationIdDetails details = taskContext.PublishedProviderVersionDetails;
            
            LogInformation($"Querying latest earlier published provider version for '{taskContext.Parameters}'");

            return await Cosmos.GetLatestEarlierPublishedProviderVersion(details.FundingStreamId,
                details.FundingPeriodId,
                details.TimeStamp,
                providerId);
        }
    }
}