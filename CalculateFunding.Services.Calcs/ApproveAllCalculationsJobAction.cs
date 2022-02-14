using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class ApproveAllCalculationsJobAction : IApproveAllCalculationsJobAction
    {
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;

        public ApproveAllCalculationsJobAction(
            IJobManagement jobManagement,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobManagement = jobManagement;
            _logger = logger;
        }

        public async Task<Job> Run(string specificationId, Reference user, string correlationId)
        {
            Job approveCalculationJob = await CreateJob(NewJobCreateModel(specificationId,
                "Approve All Calculations",
                JobConstants.DefinitionNames.ApproveAllCalculationsJob,
                correlationId,
                user,
                new Dictionary<string, string>
                {
                    {"specification-id", specificationId},
                    {"user-id", user.Id },
                    {"user-name", user.Name }
                }));

            return approveCalculationJob;
        }

        private JobCreateModel NewJobCreateModel(string specificationId, string message, string jobDefinitionId,
            string correlationId, Reference user, IDictionary<string, string> properties,
            string parentJobId = null, int? itemCount = null) =>
            new JobCreateModel
            {
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = "Specification",
                    Message = message
                },
                InvokerUserId = user.Id,
                InvokerUserDisplayName = user.Name,
                JobDefinitionId = jobDefinitionId,
                ParentJobId = parentJobId,
                SpecificationId = specificationId,
                Properties = properties,
                CorrelationId = correlationId,
                ItemCount = itemCount
            };

        private async Task<Job> CreateJob(JobCreateModel createModel)
        {
            try
            {
                var job = await _jobManagement.QueueJob(createModel);

                GuardAgainstNullJob(job, createModel);

                return job;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create job of type '{createModel.JobDefinitionId}' on specification '{createModel.Trigger.EntityId}'. {ex}");
                throw;
            }
        }

        private void GuardAgainstNullJob(Job job, JobCreateModel createModel)
        {
            if (job != null) return;

            var errorMessage = $"Creating job of type {createModel.JobDefinitionId} on specification {createModel.SpecificationId} returned no result";

            _logger.Error(errorMessage);

            throw new Exception(errorMessage);
        }
    }
}
