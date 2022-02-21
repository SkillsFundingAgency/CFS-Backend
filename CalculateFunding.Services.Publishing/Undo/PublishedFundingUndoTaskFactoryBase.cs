using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo
{
    public abstract class PublishedFundingUndoTaskFactoryBase
    {
        protected readonly IPublishedFundingUndoCosmosRepository Cosmos;
        protected readonly IPublishedFundingUndoBlobStoreRepository BlobStore;
        protected readonly IProducerConsumerFactory ProducerConsumerFactory;
        protected readonly ILogger Logger;
        protected readonly IJobTracker JobTracker;
        protected readonly IPrerequisiteCheckerLocator PrerequisiteCheckerLocator;

        protected PublishedFundingUndoTaskFactoryBase(IPublishedFundingUndoCosmosRepository cosmos,
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger, 
            IJobTracker jobTracker,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator)
        {
            Guard.ArgumentNotNull(cosmos, nameof(cosmos));
            Guard.ArgumentNotNull(blobStore, nameof(blobStore));
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));

            Cosmos = cosmos;
            BlobStore = blobStore;
            ProducerConsumerFactory = producerConsumerFactory;
            Logger = logger;
            JobTracker = jobTracker;
            PrerequisiteCheckerLocator = prerequisiteCheckerLocator;
        }   
        
        public virtual IPublishedFundingUndoJobTask CreateContextInitialisationTask()
        {
            return new PublishedFundingUndoContextInitialisationTask(Cosmos,
                BlobStore,
                ProducerConsumerFactory,
                PrerequisiteCheckerLocator,
                Logger,
                JobTracker);
        }
    }
}