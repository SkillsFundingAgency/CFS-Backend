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
    public class PublishedFundingUndoTask : UndoTaskBase, IPublishedFundingUndoJobTask
    {
        public PublishedFundingUndoTask(IPublishedFundingUndoCosmosRepository cosmos, 
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

        public bool VersionDocuments => false;

        public async Task Run(PublishedFundingUndoTaskContext taskContext)
        {
            LogStartingTask();
            
            Guard.ArgumentNotNull(taskContext?.PublishedFundingDetails, nameof(taskContext.PublishedFundingDetails));
            
            UndoTaskDetails details = taskContext.PublishedFundingDetails;

            ICosmosDbFeedIterator feed = GetPublishedFundingFeed(details);
            
            FeedContext feedContext = new FeedContext(taskContext, feed);
            
            IProducerConsumer producerConsumer = ProducerConsumerFactory.CreateProducerConsumer(ProducePublishedFunding,
                UndoPublishedFunding,
                200,
                4,
                Logger);

            await producerConsumer.Run(feedContext);

            await NotifyJobProgress(taskContext);
            
            LogCompletedTask();
        }

        protected virtual ICosmosDbFeedIterator GetPublishedFundingFeed(UndoTaskDetails details) =>
            Cosmos.GetPublishedFunding(details.FundingStreamId,
                details.FundingPeriodId,
                details.TimeStamp);

        private async Task<(bool isComplete, IEnumerable<PublishedFunding> items)> ProducePublishedFunding(CancellationToken cancellationToken,
            dynamic context)
        {
            return await GetDocumentsFromFeed<PublishedFunding>(cancellationToken, context);
        }

        protected  async Task UndoPublishedFunding(CancellationToken cancellationToken,
            dynamic context, 
            IEnumerable<PublishedFunding> publishedFunding)
        {
            PublishedFundingUndoTaskContext taskContext = GetTaskContext(context);
            List<PublishedFunding> publishedFundingToDelete = new List<PublishedFunding>();
            List<PublishedFunding> publishedFundingToUpdate = new List<PublishedFunding>();
            
            foreach (PublishedFunding document in publishedFunding)
            {
                PublishedFundingVersion previousVersion = await GetPreviousPublishedFundingVersion(document.Current, taskContext);

                if (previousVersion == null)
                {
                    publishedFundingToDelete.Add(document);
                }
                else
                {
                    document.Current = previousVersion;
                    publishedFundingToUpdate.Add(document);
                }
            }

            Task[] updateTasks = new[]
            {
                UpdateDocuments(publishedFundingToUpdate, _ => _.ParitionKey),
                DeleteDocuments(publishedFundingToDelete, _ => _.ParitionKey, IsHardDelete)
            };

            await TaskHelper.WhenAllAndThrow(updateTasks);
        }
        
        protected virtual async Task<PublishedFundingVersion> GetPreviousPublishedFundingVersion(PublishedFundingVersion currentVersion, 
            PublishedFundingUndoTaskContext taskContext)
        {
            LogInformation($"Querying latest earlier published funding version for '{taskContext.Parameters}'");
            
            UndoTaskDetails details = taskContext.PublishedFundingVersionDetails;

            return await Cosmos.GetLatestEarlierPublishedFundingVersion(details.FundingStreamId,
                details.FundingPeriodId,
                details.TimeStamp,
                currentVersion.OrganisationGroupTypeIdentifier,
                currentVersion.OrganisationGroupIdentifierValue,
                currentVersion.GroupingReason);
        }
    }
}