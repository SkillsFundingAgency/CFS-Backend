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
    public class JobCreationForSpecification<TJobDefinition> : ICreateJobsForSpecifications<TJobDefinition>
        where TJobDefinition : IJobDefinition
    {
        private readonly IJobsApiClient _jobs;
        private readonly IPublishingResiliencePolicies _resiliencePolicies;
        private readonly ILogger _logger;
        private readonly IJobDefinition _jobDefinition;

        public JobCreationForSpecification(IJobsApiClient jobs,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger,
            IJobDefinition jobDefinition)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobDefinition, nameof(jobDefinition));

            _jobs = jobs;
            _resiliencePolicies = resiliencePolicies;
            _logger = logger;
            _jobDefinition = jobDefinition;
        }

        private Policy JobsPolicy => _resiliencePolicies.JobsApiClient;

        public async Task<Job> CreateJob(string specificationId,
            Reference user,
            string correlationId,
            Dictionary<string, string> properties = null,
            string messageBody = null)
        {
            Dictionary<string, string> messageProperties =
                properties ?? new Dictionary<string, string>();

            messageProperties.Add("specification-id", specificationId);
            messageProperties.Add("user-id", user.Id);
            messageProperties.Add("user-name", user.Name);

            try
            {
                Job job = await JobsPolicy.ExecuteAsync(() => _jobs.CreateJob(new JobCreateModel
                {
                    InvokerUserDisplayName = user.Name,
                    InvokerUserId = user.Id,
                    JobDefinitionId = _jobDefinition.Id,
                    Properties = messageProperties,
                    MessageBody = messageBody ?? string.Empty,
                    SpecificationId = specificationId,
                    Trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = nameof(Specification),
                        Message = _jobDefinition.TriggerMessage
                    },
                    CorrelationId = correlationId
                }));

                if (job != null)
                {
                    _logger.Information($"New job of type '{job.JobDefinitionId}' created with id: '{job.Id}'");
                }
                else
                {
                    string errorMessage = $"Failed to create job of type '{job.JobDefinitionId}' on specification '{specificationId}'";

                    _logger.Error(errorMessage);
                }

                return job;
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