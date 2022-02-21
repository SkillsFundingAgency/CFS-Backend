using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using Serilog;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public class DsgRollBackTaskFactory : PublishedFundingUndoTaskFactoryBase, IPublishedFundingUndoTaskFactory
    {
        public DsgRollBackTaskFactory(IPublishedFundingUndoCosmosRepository cosmos,
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger,
            IJobTracker jobTracker,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator) : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker, prerequisiteCheckerLocator)
        {
        }

        public bool IsForJob(PublishedFundingUndoJobParameters parameters) => true;

        public override IPublishedFundingUndoJobTask CreateContextInitialisationTask()
        => new DsgRollBackInitialisationTask(Cosmos,
            BlobStore,
            ProducerConsumerFactory,
            Logger,
            JobTracker);

        public IPublishedFundingUndoJobTask CreatePublishedProviderUndoTask()
            => new DsgRollBackPublishedProviderUndoTask(Cosmos,
                BlobStore,
                ProducerConsumerFactory,
                Logger,
                JobTracker);

        public IPublishedFundingUndoJobTask CreatePublishedProviderVersionUndoTask()
            => new DsgRollBackPublishedProviderVersionUndoTask(Cosmos,
                BlobStore,
                ProducerConsumerFactory,
                Logger,
                JobTracker);

        public IPublishedFundingUndoJobTask CreatePublishedFundingUndoTask()
            => new DsgRollBackPublishedFundingUndoTask(Cosmos,
                BlobStore,
                ProducerConsumerFactory,
                Logger,
                JobTracker);

        public IPublishedFundingUndoJobTask CreatePublishedFundingVersionUndoTask()  
            => new DsgRollBackPublishedFundingVersionUndoTask(Cosmos,
            BlobStore,
            ProducerConsumerFactory,
            Logger,
            JobTracker);

        public IEnumerable<IPublishedFundingUndoJobTask> CreateUndoTasks(PublishedFundingUndoTaskContext taskContext)
        {
            List<IPublishedFundingUndoJobTask> undoTasks = new List<IPublishedFundingUndoJobTask>();

            AddWithNullCheck(taskContext.UndoTaskDetails, undoTasks, CreatePublishedFundingUndoTask);
            AddWithNullCheck(taskContext.UndoTaskDetails, undoTasks, CreatePublishedFundingVersionUndoTask);
            AddWithNullCheck(taskContext.UndoTaskDetails, undoTasks, CreatePublishedProviderUndoTask);
            AddWithNullCheck(taskContext.UndoTaskDetails, undoTasks, CreatePublishedProviderVersionUndoTask);

            return undoTasks;
        }

        private void AddWithNullCheck(object property, ICollection<IPublishedFundingUndoJobTask> undoTasks, Func<IPublishedFundingUndoJobTask> createTask)
        {
            if (property == null)
            {
                return;
            }

            undoTasks.Add(createTask());
        }
    }
}