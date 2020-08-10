using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs
{
    public class QueueEditSpecificationJobActions: IQueueEditSpecificationJobActions
    {
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;

        public QueueEditSpecificationJobActions(
            IJobManagement jobManagement,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobManagement = jobManagement;
            _logger = logger;
        }

        public async Task Run(SpecificationVersion specificationVersion, Reference user, string correlationId)
        {
            string errorMessage = $"Unable to queue ProviderSnapshotDataLoadJob for specification - {specificationVersion.SpecificationId}";
            Reference fundingStream = specificationVersion.FundingStreams.FirstOrDefault();
            if (fundingStream != null && specificationVersion.ProviderSource == Models.Providers.ProviderSource.FDZ)
            {
                Job createProviderSnapshotDataLoadJob = await CreateJob(errorMessage,
                NewJobCreateModel(specificationVersion.SpecificationId,
                    "Assigning ProviderVersionId for specification",
                    JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob,
                    correlationId,
                    user,
                    new Dictionary<string, string>
                    {
                        {"specification-id", specificationVersion.SpecificationId},
                        {"fundingstream-id", fundingStream.Id},
                        {"providerSanpshot-id", specificationVersion.ProviderSnapshotId?.ToString() }
                    }));

                GuardAgainstNullJob(createProviderSnapshotDataLoadJob, $"Unable to queue ProviderSnapshotDataLoadJob for specification - {specificationVersion.SpecificationId}");
            }
        }

        private JobCreateModel NewJobCreateModel(string specificationId,
            string message,
            string jobDefinitionId,
            string correlationId,
            Reference user,
            IDictionary<string, string> properties,
            string parentJobId = null,
            int? itemCount = null)
        {
            return new JobCreateModel
            {
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = nameof(Specification),
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
        }

        private async Task<Job> CreateJob(string errorMessage, JobCreateModel createModel)
        {
            try
            {
                Job job = await _jobManagement.QueueJob(createModel);

                return job;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, errorMessage);

                throw new Exception(errorMessage);
            }
        }

        private void GuardAgainstNullJob(Job job, string message)
        {
            if (job != null) return;

            _logger.Error(message);

            throw new Exception(message);
        }
    }
}
