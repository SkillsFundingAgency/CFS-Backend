using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo.Tasks
{
    public class PublishedFundingUndoContextInitialisationTask : UndoTaskBase, IPublishedFundingUndoJobTask
    {
        public PublishedFundingUndoContextInitialisationTask(IPublishedFundingUndoCosmosRepository cosmos, 
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
        }

        public async Task Run(PublishedFundingUndoTaskContext taskContext)
        {
            Guard.ArgumentNotNull(taskContext, nameof(taskContext));

            string correlationId = taskContext.Parameters.ForCorrelationId;

            LogInformation($"Initialising task context for correlationId: {correlationId}");

            await InitialiseFundingDetails(correlationId, taskContext);

            if (taskContext.UndoTaskDetails != null)
            {
                taskContext.UndoTaskDetails.CorrelationId = correlationId;
            }

            EnsureContextInitialised(taskContext);
        }

        private static void EnsureContextInitialised(PublishedFundingUndoTaskContext taskContext)
        {
            (bool isInitiliased, IEnumerable<string> errors) initialisationCheck = taskContext.EnsureIsInitialised();

            if (!initialisationCheck.isInitiliased)
            {
                throw new InvalidOperationException(initialisationCheck.errors.Join("\n"));
            }
        }
        
        private async Task InitialiseFundingDetails(string correlationId, PublishedFundingUndoTaskContext context)
        {
            context.UndoTaskDetails = await Cosmos.GetCorrelationIdDetailsForPublishedProviderVersions(correlationId);
        }
    }
}