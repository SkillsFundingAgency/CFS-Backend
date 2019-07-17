using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using Policy = Polly.Policy;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public abstract class JobCreationForSpecification
    {
        private readonly IJobsApiClient _jobs;
        private readonly IPublishingResiliencePolicies _resiliencePolicies;
        private readonly ILogger _logger;
        private readonly string _triggerMessage;
        private readonly string _jobDefinitionId;

        protected JobCreationForSpecification(IJobsApiClient jobs,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger,
            string triggerMessage,
            string jobDefinitionId)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.IsNullOrWhiteSpace(jobDefinitionId, nameof(jobDefinitionId));
            Guard.IsNullOrWhiteSpace(triggerMessage, nameof(triggerMessage));

            _jobs = jobs;
            _resiliencePolicies = resiliencePolicies;
            _logger = logger;
            _triggerMessage = triggerMessage;
            _jobDefinitionId = jobDefinitionId;
        }

        private Policy JobsPolicy => _resiliencePolicies.JobsApiClient;

        public async Task<Job> CreateJob(string specificationId,
            Reference user,
            string correlationId)
        {
            try
            {
                return await JobsPolicy.ExecuteAsync(() => _jobs.CreateJob(new JobCreateModel
                {
                    InvokerUserDisplayName = user.Name,
                    InvokerUserId = user.Id,
                    JobDefinitionId = _jobDefinitionId,
                    Properties = new Dictionary<string, string>
                    {
                        {"specification-id", specificationId}
                    },
                    SpecificationId = specificationId,
                    Trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = nameof(Specification),
                        Message = _triggerMessage
                    },
                    CorrelationId = correlationId
                }));
            }
            catch (Exception ex)
            {
                string error = $"Failed to queue publishing of specification with id: {specificationId}";

                _logger.Error(ex, error);

                throw new Exception(error);
            }
        }
    }
}