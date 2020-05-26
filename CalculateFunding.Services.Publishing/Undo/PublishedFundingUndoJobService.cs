using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class PublishedFundingUndoJobService : IPublishedFundingUndoJobService
    {
        private const string PublishedFundingUndoJob = JobConstants.DefinitionNames.PublishedFundingUndoJob;

        private readonly IPublishedFundingUndoJobCreation _jobCreation;
        private readonly IPublishedFundingUndoTaskFactoryLocator _factoryLocator;
        private readonly IJobTracker _jobTracker;
        private readonly ILogger _logger;

        public PublishedFundingUndoJobService(IPublishedFundingUndoTaskFactoryLocator factoryLocator,
            IJobTracker jobTracker,
            IPublishedFundingUndoJobCreation jobCreation,
            ILogger logger)
        {
            Guard.ArgumentNotNull(factoryLocator, nameof(factoryLocator));
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(jobCreation, nameof(jobCreation));
            Guard.ArgumentNotNull(logger, nameof(logger));
        
            _factoryLocator = factoryLocator;
            _jobTracker = jobTracker;
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

        public async Task Run(Message message)
        {
            PublishedFundingUndoJobParameters parameters = message;

            try
            {
                LogInformation($"Starting job tracking for {parameters}");
            
                if (!await _jobTracker.TryStartTrackingJob(parameters.JobId, PublishedFundingUndoJob))
                {
                    return;
                }
            
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
                    undoPublishedFundingTask.Run(taskContext),
                    undoPublishedFundingVersionTask.Run(taskContext),
                    undoPublishedProviderTask.Run(taskContext),
                    undoPublishedProviderVersionTask.Run(taskContext)
                };

                await TaskHelper.WhenAllAndThrow(undoTasks);
                
                EnsureNoTaskErrors(taskContext);
            
                LogInformation($"Completed {PublishedFundingUndoJob}. Completing job tracking.");

                await _jobTracker.CompleteTrackingJob(parameters.JobId);
            }
            catch (Exception e)
            {
                string errorMessage = $"Unable to complete {PublishedFundingUndoJob} for correlationId: {parameters.ForCorrelationId}.\n{e.Message}";
                
                await _jobTracker.FailJob(errorMessage, 
                    parameters.JobId);
                
                LogError(e, errorMessage);
                
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

        private void LogInformation(string message) => _logger.Information(message);

        private void LogError(Exception exception, string message) => _logger.Error(exception, message);
    }
}