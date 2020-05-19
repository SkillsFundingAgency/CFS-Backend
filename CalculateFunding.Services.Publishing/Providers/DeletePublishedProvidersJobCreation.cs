using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class DeletePublishedProvidersJobCreation : ICreateDeletePublishedProvidersJobs
    {
        private const string DeletePublishedProvidersJob = JobConstants.DefinitionNames.DeletePublishedProvidersJob;
        private readonly ILogger _logger;
        private readonly IJobManagement _jobManagement;

        public DeletePublishedProvidersJobCreation(
            IJobManagement jobManagement,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobManagement = jobManagement;
            _logger = logger;
        }
        
        public async Task<Job> CreateJob(string fundingStreamId, 
            string fundingPeriodId, 
            string correlationId)
        {
            Dictionary<string, string> messageProperties = new Dictionary<string, string>
            {
                {"funding-stream-id", fundingStreamId}, 
                {"funding-period-id", fundingPeriodId}
            };

            try
            {
                Job job = await _jobManagement.QueueJob(new JobCreateModel
                {
                    JobDefinitionId = DeletePublishedProvidersJob,
                    Properties = messageProperties,
                    Trigger = new Trigger
                    {
                        EntityId = "N/A",
                        Message = $"Requested deletion of published providers for funding stream {fundingStreamId} and funding period {fundingPeriodId}"
                    },
                    CorrelationId = correlationId
                });

                if (job != null)
                {
                    _logger.Information($"New job of type '{DeletePublishedProvidersJob}' created with id: '{job.Id}'");
                }
                else
                {
                    _logger.Error(
                        $"Failed to create job of type '{DeletePublishedProvidersJob}' on funding stream '{fundingStreamId}' and funding period {fundingPeriodId}");
                }

                return job;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to create job of type '{DeletePublishedProvidersJob}' on funding stream '{fundingStreamId}' and funding period {fundingPeriodId}");

                throw;
            }
        }
    }
}