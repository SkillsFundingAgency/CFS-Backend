using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class PublishedFundingUndoJobService : JobProcessingService, IPublishedFundingUndoJobService
    {
        private const string PublishedFundingUndoJob = JobConstants.DefinitionNames.PublishedFundingUndoJob;
        private readonly IPublishedFundingUndoJobCreation _jobCreation;
        private readonly IPublishedFundingUndoTaskFactoryLocator _factoryLocator;
        private readonly ILogger _logger;

        public PublishedFundingUndoJobService(IPublishedFundingUndoTaskFactoryLocator factoryLocator,
            IJobManagement jobManagement,
            IPublishedFundingUndoJobCreation jobCreation,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(factoryLocator, nameof(factoryLocator));
            Guard.ArgumentNotNull(jobCreation, nameof(jobCreation));
            Guard.ArgumentNotNull(logger, nameof(logger));
        
            _factoryLocator = factoryLocator;
            _logger = logger;
            _jobCreation = jobCreation;
        }

        public async Task<Job> QueueJob(string forCorrelationId,
            bool isHardDelete,
            Reference user,
            string correlationId)
        {
            Guard.IsNullOrWhiteSpace(forCorrelationId, nameof(forCorrelationId));
            Guard.ArgumentNotNull(user, nameof(user));

            return await _jobCreation.CreateJob(forCorrelationId,
                isHardDelete,
                user,
                correlationId);
        }

        public override async Task Process(Message message)
        {
            PublishedFundingUndoJobParameters parameters = message;

            await PerformUndo(parameters);
        }

        protected async Task PerformUndo(PublishedFundingUndoJobParameters parameters)
        {
            try
            {
                IPublishedFundingUndoTaskFactory taskFactory = _factoryLocator.GetTaskFactoryFor(parameters);

                IPublishedFundingUndoJobTask initialiseContextTask = taskFactory.CreateContextInitialisationTask();
                IPublishedFundingUndoJobTask undoPublishedProviderTask = taskFactory.CreatePublishedProviderUndoTask();
                IPublishedFundingUndoJobTask undoPublishedProviderVersionTask = taskFactory.CreatePublishedProviderVersionUndoTask();
                IPublishedFundingUndoJobTask undoPublishedFundingTask = taskFactory.CreatePublishedFundingUndoTask();
                IPublishedFundingUndoJobTask undoPublishedFundingVersionTask = taskFactory.CreatePublishedFundingVersionUndoTask();

                PublishedFundingUndoTaskContext taskContext = new PublishedFundingUndoTaskContext(parameters);

                await initialiseContextTask.Run(taskContext);

                Task[] undoTasks = new[]
                {
                    undoPublishedFundingTask.Run(taskContext), undoPublishedFundingVersionTask.Run(taskContext), undoPublishedProviderTask.Run(taskContext), undoPublishedProviderVersionTask.Run(taskContext)
                };

                await TaskHelper.WhenAllAndThrow(undoTasks);

                EnsureNoTaskErrors(taskContext);
            }
            catch(Exception e)
            {
                string errorMessage = $"Unable to complete {PublishedFundingUndoJob} for correlationId: {parameters.ForCorrelationId}.\n{e.Message}";
                throw new NonRetriableException(errorMessage, e);
            }
        }

        private void EnsureNoTaskErrors(PublishedFundingUndoTaskContext taskContext)
        {
            if (taskContext.Errors.IsNullOrEmpty())
            {
                return;
            }
            
            throw new InvalidOperationException("Undo tasks generated unhandled exceptions", 
                new AggregateException(taskContext.Errors));
        }
    }
}