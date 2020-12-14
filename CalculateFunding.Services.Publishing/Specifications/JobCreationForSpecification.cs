using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public abstract class JobCreationForSpecification : ICreatePublishIntegrityJob
    {
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;

        protected JobCreationForSpecification(IJobManagement jobManagement,
            ILogger logger,
            string jobDefinitionId,
            string triggerMessage)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.IsNullOrWhiteSpace(jobDefinitionId, nameof(jobDefinitionId));

            _jobManagement = jobManagement;
            _logger = logger;
            JobDefinitionId = jobDefinitionId;
            TriggerMessage = triggerMessage;
        }
        
        public string JobDefinitionId { get; }
                                                     
        public string TriggerMessage { get; }

        public async Task<Job> CreateJob(string specificationId,
            Reference user,
            string correlationId,
            Dictionary<string, string> properties = null,
            string messageBody = null,
            string parentJobId = null,
            bool compress = false)
        {
            Dictionary<string, string> messageProperties =
                properties ?? new Dictionary<string, string>();

            messageProperties.Add("specification-id", specificationId);
            messageProperties.Add("user-id", user.Id);
            messageProperties.Add("user-name", user.Name);

            AddExtraMessageProperties(messageProperties);

            try
            {
                Job job = await _jobManagement.QueueJob(new JobCreateModel
                {
                    InvokerUserDisplayName = user.Name,
                    InvokerUserId = user.Id,
                    JobDefinitionId = JobDefinitionId,
                    Properties = messageProperties,
                    MessageBody = messageBody ?? string.Empty,
                    SpecificationId = specificationId,
                    Trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = "Specification",
                        Message = TriggerMessage
                    },
                    CorrelationId = correlationId,
                    Compress = compress
                });

                if (!string.IsNullOrWhiteSpace(parentJobId))
                {
                    job.ParentJobId = parentJobId;
                }

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
        
        protected virtual void AddExtraMessageProperties(Dictionary<string, string> messageProperties){}
    }
}