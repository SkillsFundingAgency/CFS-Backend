using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo.Tasks
{
    public abstract class UndoTaskBase
    {
        public IPublishedFundingUndoCosmosRepository Cosmos { get; }

        public IPublishedFundingUndoBlobStoreRepository BlobStore { get; }

        public IProducerConsumerFactory ProducerConsumerFactory { get; }

        public ILogger Logger { get; }

        public IJobTracker JobTracker { get; }

        protected UndoTaskBase(IPublishedFundingUndoCosmosRepository cosmos,
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger, 
            IJobTracker jobTracker)
        {
            Guard.ArgumentNotNull(cosmos, nameof(cosmos));
            Guard.ArgumentNotNull(blobStore, nameof(blobStore));
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));

            Cosmos = cosmos;
            BlobStore = blobStore;
            ProducerConsumerFactory = producerConsumerFactory;
            Logger = logger;
            JobTracker = jobTracker;
        }

        protected interface IFeedContext
        {
            PublishedFundingUndoTaskContext TaskContext { get; set; }
        }

        protected class FeedContext<TDocument> : IFeedContext where TDocument : IIdentifiable
        {
            public FeedContext(PublishedFundingUndoTaskContext taskContext,
                ICosmosDbFeedIterator<TDocument> feed)
            {
                TaskContext = taskContext;
                Feed = feed;
            }

            public PublishedFundingUndoTaskContext TaskContext { get; set; }

            public ICosmosDbFeedIterator<TDocument> Feed { get; set; }
        }

        protected async Task<(bool isComplete, IEnumerable<TDocument> items)> GetDocumentsFromFeed<TDocument>(CancellationToken cancellationToken,
            dynamic context)
            where TDocument : IIdentifiable
        {
            try
            {
                ICosmosDbFeedIterator<TDocument> feed = ((FeedContext<TDocument>) context).Feed;
            
                LogInformation($"Requesting next page of {typeof(TDocument).Name} documents from cosmos feed");

                if (!feed.HasMoreResults)
                {
                    LogInformation($"{typeof(TDocument).Name} document feed has no more records. Completing producer");
                
                    return (true, ArraySegment<TDocument>.Empty);
                }

                IEnumerable<TDocument> documents = await feed.ReadNext(cancellationToken);

                while(documents.IsNullOrEmpty() && feed.HasMoreResults)
                {
                    documents = await feed.ReadNext(cancellationToken);
                }

                if (documents.IsNullOrEmpty() && !feed.HasMoreResults)
                {
                    LogInformation($"{typeof(TDocument).Name} document feed has no more records. Completing producer");
                
                    return (true, ArraySegment<TDocument>.Empty);    
                }

                LogInformation($"{typeof(TDocument).Name} document feed produced next {documents.Count()} documents.");
            
                return (false, documents);
            }
            catch (Exception exception)
            {
                LogError(exception, "Unable to get documents from cosmos feed");

                return (true, ArraySegment<TDocument>.Empty);
            }
        }

        protected async Task DeleteDocuments<TDocument>(IEnumerable<TDocument> documents, 
            Func<TDocument, string> partitionKeyAccessor,
            bool hardDelete = false)
            where TDocument : IIdentifiable
        {
            if (!documents.Any())
            {
                return;
            }
            
            LogInformation($"Requesting bulk deletion of {documents.Count()} {typeof(TDocument).Name} documents. HardDelete {hardDelete}");
            
            await Cosmos.BulkDeletePublishedFundingDocuments(documents, partitionKeyAccessor, hardDelete);
        }
        
        protected async Task UpdateDocuments<TDocument>(IEnumerable<TDocument> documents, 
            Func<TDocument, string> partitionKeyAccessor)
            where TDocument : IIdentifiable
        {
            if (!documents.Any())
            {
                return;
            }
            
            LogInformation($"Requesting bulk update of {documents.Count()} {typeof(TDocument).Name} documents");
            
            await Cosmos.BulkUpdatePublishedFundingDocuments(documents, partitionKeyAccessor);
        }

        protected PublishedFundingUndoTaskContext GetTaskContext(dynamic context) => ((IFeedContext)context).TaskContext;

        protected void LogStartingTask() => LogInformation($"Starting task");
        
        protected void LogCompletedTask() => LogInformation($"Completed task");
        
        protected void LogInformation(string message) => Logger.Information(FormattedLogMessage(message));

        protected void LogError(Exception exception, string message) => Logger.Error(exception, FormattedLogMessage(message));

        private string FormattedLogMessage(string message) => $"[{GetType().Name}] {message}";

        protected async Task NotifyJobProgress(PublishedFundingUndoTaskContext taskContext)
        {
            taskContext.IncrementCompletedTaskCount();

            await JobTracker.NotifyProgress(taskContext.CompletedTaskCount, taskContext.Parameters.JobId);
        }
    }
}