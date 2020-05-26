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
    public class SoftDeletePublishedProviderVersionsUndoTask : PublishedProviderVersionsUndoTaskBase
    {
        public SoftDeletePublishedProviderVersionsUndoTask(IPublishedFundingUndoCosmosRepository cosmos, 
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
            Task[] undoTasks = new[]
            {
                DeleteDocuments(publishedProviderVersions, _ => _.PartitionKey),
                DeleteBlobDocuments(publishedProviderVersions)
            };

            await TaskHelper.WhenAllAndThrow(undoTasks);
        }
    }
}