using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class DeletePublishedProvidersJobCreation : ICreateDeletePublishedProvidersJobs
    {
        private const string DeletePublishedProvidersJob = JobConstants.DefinitionNames.DeletePublishedProvidersJob;
        private readonly IJobsApiClient _jobs;
        private readonly Policy _resiliencePolicy;
        private readonly ILogger _logger;

        public DeletePublishedProvidersJobCreation(IJobsApiClient jobs,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobs = jobs;
            _resiliencePolicy = resiliencePolicies.JobsApiClient;
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
                Job job = await _resiliencePolicy.ExecuteAsync(() => _jobs.CreateJob(new JobCreateModel
                {
                    JobDefinitionId = DeletePublishedProvidersJob,
                    Properties = messageProperties,
                    Trigger = new Trigger
                    {
                        EntityId = "N/A",
                        Message = $"Requested deletion of published providers for funding stream {fundingStreamId} and funding period {fundingPeriodId}"
                    },
                    CorrelationId = correlationId
                }));

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