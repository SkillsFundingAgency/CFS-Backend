using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo.Tasks
{
    public class HardDeletePublishedProviderVersionsUndoTask : PublishedProviderVersionsUndoTaskBase
    {
        public HardDeletePublishedProviderVersionsUndoTask(IPublishedFundingUndoCosmosRepository cosmos, 
            IPublishedFundingUndoBlobStoreRepository blobStore, 
            IProducerConsumerFactory producerConsumerFactory, 
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
        }
        
        protected override async Task UndoPublishedProviderVersions(CancellationToken cancellationToken, 
            dynamic context, 
            IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
            string apiVersion = ((CalculateFunding.Services.Publishing.Undo.Tasks.UndoTaskBase.FeedContext)context).TaskContext.Parameters.ForApiVersion;
            string channelCodes = ((CalculateFunding.Services.Publishing.Undo.Tasks.UndoTaskBase.FeedContext)context).TaskContext.Parameters.ForChannelCodes;
            Task[] undoTasks = new[]
            {
                DeleteDocuments(publishedProviderVersions, _ => _.PartitionKey, true),
                DeleteBlobDocuments(publishedProviderVersions, apiVersion, channelCodes)
            };

            await TaskHelper.WhenAllAndThrow(undoTasks);
        }
    }
}