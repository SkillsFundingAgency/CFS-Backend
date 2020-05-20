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
            
            Task[] initialisations = new[]
            {
                InitialisePublishedProviderDetails(correlationId, taskContext),
                InitialisePublishedProviderVersionDetails(correlationId, taskContext),
                InitialisePublishFundingDetails(correlationId, taskContext),
                InitialisePublishFundingVersionDetails(correlationId, taskContext),
            };

            await TaskHelper.WhenAllAndThrow(initialisations);

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

        private async Task InitialisePublishFundingDetails(string correlationId, PublishedFundingUndoTaskContext context)
        {
            context.PublishedFundingDetails = await Cosmos.GetCorrelationIdDetailsForPublishedFunding(correlationId);
        }
        
        private async Task InitialisePublishFundingVersionDetails(string correlationId, PublishedFundingUndoTaskContext context)
        {
            context.PublishedFundingVersionDetails = await Cosmos.GetCorrelationIdDetailsForPublishedFundingVersions(correlationId);
        }
        
        private async Task InitialisePublishedProviderDetails(string correlationId, PublishedFundingUndoTaskContext context)
        {
            context.PublishedProviderDetails = await Cosmos.GetCorrelationDetailsForPublishedProviders(correlationId);
        }
        
        private async Task InitialisePublishedProviderVersionDetails(string correlationId, PublishedFundingUndoTaskContext context)
        {
            context.PublishedProviderVersionDetails = await Cosmos.GetCorrelationIdDetailsForPublishedProviderVersions(correlationId);
        }
    }
}