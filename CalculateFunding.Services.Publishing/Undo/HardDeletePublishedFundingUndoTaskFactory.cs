using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class HardDeletePublishedFundingUndoTaskFactory : PublishedFundingUndoTaskFactoryBase, IPublishedFundingUndoTaskFactory
    {
        public HardDeletePublishedFundingUndoTaskFactory(IPublishedFundingUndoCosmosRepository cosmos, 
            IPublishedFundingUndoBlobStoreRepository blobStore, 
            IProducerConsumerFactory producerConsumerFactory, 
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
        }

        public bool IsForJob(PublishedFundingUndoJobParameters parameters)
        {
            return parameters.IsHardDelete;
        }

        public IPublishedFundingUndoJobTask CreatePublishedProviderUndoTask()
        {
            return new PublishedProviderUndoTask(Cosmos,
                BlobStore,
                ProducerConsumerFactory,
                Logger,
                JobTracker,
                isHardDelete: true);
        }

        public IPublishedFundingUndoJobTask CreatePublishedProviderVersionUndoTask()
        {
            return new HardDeletePublishedProviderVersionsUndoTask(Cosmos,
                BlobStore,
                ProducerConsumerFactory,
                Logger,
                JobTracker);
        }

        public IPublishedFundingUndoJobTask CreatePublishedFundingUndoTask()
        {
            return new PublishedFundingUndoTask(Cosmos,
                BlobStore,
                ProducerConsumerFactory,
                Logger,
                JobTracker,
                isHardDelete: true);
        }

        public IPublishedFundingUndoJobTask CreatePublishedFundingVersionUndoTask()
        {
            return new HardDeletePublishedFundingVersionUndoTask(Cosmos,
                BlobStore,
                ProducerConsumerFactory,
                Logger,
                JobTracker);
        }
    }
}