using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class PublishedFundingUndoJobCreation : IPublishedFundingUndoJobCreation
    {
        private const string PublishedFundingUndoJob = JobConstants.DefinitionNames.PublishedFundingUndoJob;
        private const string ForCorrelationIdPropertyName = PublishedFundingUndoJobParameters.ForCorrelationIdPropertyName;
        private const string ForSpecificationIdPropertyName = PublishedFundingUndoJobParameters.ForSpecificationIdPropertyName;
        private const string IsHardDeletePropertyName = PublishedFundingUndoJobParameters.IsHardDeletePropertyName;
        
        private readonly IJobsApiClient _jobs;
        private readonly AsyncPolicy _resilience;
        private readonly ILogger _logger;

        public PublishedFundingUndoJobCreation(IJobsApiClient jobs,
            IPublishingResiliencePolicies resilience,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(resilience?.JobsApiClient, nameof(resilience.JobsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobs = jobs;
            _resilience = resilience.JobsApiClient;
            _logger = logger;
        }

        public async Task<Job> CreateJob(string forCorrelationId,
            string specificationId,
            bool isHardDelete,
            Reference user,
            string correlationId)
        {
            try
            {
                Job job = await _resilience.ExecuteAsync(() => _jobs.CreateJob(new JobCreateModel
                {
                    InvokerUserDisplayName = user.Name,
                    InvokerUserId = user.Id,
                    JobDefinitionId = PublishedFundingUndoJob,
                    SpecificationId = specificationId,
                    Properties = new Dictionary<string, string>
                    {
                        {ForCorrelationIdPropertyName, forCorrelationId},
                        {ForSpecificationIdPropertyName, specificationId },
                        {IsHardDeletePropertyName, isHardDelete.ToString()},
                        {"user-id", user.Id},
                        {"user-name", user.Name}
                    },
                    Trigger = new Trigger
                    {
                        Message = $"Rollback publishing since correlation Id {forCorrelationId}"
                    },
                    CorrelationId = correlationId
                }));

                if (job != null)
                {
                    _logger.Information($"New job of type '{PublishedFundingUndoJob}' created with id: '{job.Id}'");
                }
                else
                {
                    _logger.Error($"Failed to create job of type '{PublishedFundingUndoJob}' for correlation id '{forCorrelationId}'");
                }

                return job;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to queue published funding undo job for correlation id: {forCorrelationId}");

                throw;
            }
        }
    }
}