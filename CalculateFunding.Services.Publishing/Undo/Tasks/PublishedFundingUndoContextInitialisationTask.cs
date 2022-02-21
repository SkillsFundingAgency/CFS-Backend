using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using Serilog;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Undo.Tasks
{
    public class PublishedFundingUndoContextInitialisationTask : UndoTaskBase, IPublishedFundingUndoJobTask
    {
        private readonly IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        
        public PublishedFundingUndoContextInitialisationTask(IPublishedFundingUndoCosmosRepository cosmos, 
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator,
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));

            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
        }

        public async Task Run(PublishedFundingUndoTaskContext taskContext)
        {
            Guard.ArgumentNotNull(taskContext, nameof(taskContext));

            string correlationId = taskContext.Parameters.ForCorrelationId;

            LogInformation($"Initialising task context for correlationId: {correlationId}");

            await InitialiseFundingDetails(correlationId, taskContext);

            if (taskContext.UndoTaskDetails != null)
            {
                IPrerequisiteChecker undoPublishingPrereqChecker = _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.UndoPublishing);

                taskContext.UndoTaskDetails.SpecificationId = taskContext.Parameters.ForSpecificationId;
                
                taskContext.UndoTaskDetails.CorrelationId = correlationId;

                await undoPublishingPrereqChecker.PerformChecks(taskContext.UndoTaskDetails, taskContext.Parameters.JobId, null, null);
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