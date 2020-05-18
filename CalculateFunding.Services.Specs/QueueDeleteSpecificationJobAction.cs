using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Specs.Interfaces;
using Polly;
using Serilog;
using Job = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using JobCreateModel = CalculateFunding.Common.ApiClient.Jobs.Models.JobCreateModel;
using Trigger = CalculateFunding.Common.ApiClient.Jobs.Models.Trigger;

namespace CalculateFunding.Services.Specs
{
    public class QueueDeleteSpecificationJobAction : IQueueDeleteSpecificationJobActions
    {
        private readonly IJobsApiClient _jobsApiClient;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _jobClientResiliencePolicy;
        private readonly Dictionary<string, string> _specificationChildJobDefinitions = new Dictionary<string, string>
        {
            [JobConstants.DefinitionNames.DeleteCalculationResultsJob] = "Deleting Calculation Results",
            [JobConstants.DefinitionNames.DeleteCalculationsJob] = "Deleting Calculations",
            [JobConstants.DefinitionNames.DeleteDatasetsJob] = "Deleting Datasets",
            [JobConstants.DefinitionNames.DeleteTestResultsJob] = "Deleting Test Results",
            [JobConstants.DefinitionNames.DeleteTestsJob] = "Deleting Tests",
            [JobConstants.DefinitionNames.DeleteJobsJob] = "Deleting Jobs"
        };

        public QueueDeleteSpecificationJobAction(
            IJobsApiClient jobs,
            ISpecificationsResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobsApiClient = jobs;
            _logger = logger;
            _jobClientResiliencePolicy = resiliencePolicies.JobsApiClient;
        }

        public async Task Run(string specificationId, Reference user, string correlationId, DeletionType deletionType)
        {
            string deletionTypeValue = deletionType.ToString("D");

            var deleteSpecificationJob = await CreateJob(NewJobCreateModel(specificationId,
                "Deleting Specification",
                JobConstants.DefinitionNames.DeleteSpecificationJob,
                correlationId,
                user,
                new Dictionary<string, string>
                {
                    {"specification-id", specificationId},
                    {"deletion-type", deletionTypeValue}
                }));

            IEnumerable<Task> specificationChildJobs = _specificationChildJobDefinitions.Select(childJob => CreateJob(
                    NewJobCreateModel(specificationId,
                        childJob.Value,
                        childJob.Key,
                        correlationId,
                        user,
                        new Dictionary<string, string>
                        {
                            {"specification-id", specificationId},
                            {"deletion-type", deletionTypeValue}
                        },
                        deleteSpecificationJob.Id)));

            await TaskHelper.WhenAllAndThrow(specificationChildJobs.ToArray());
        }

        private JobCreateModel NewJobCreateModel(string specificationId, string message, string jobDefinitionId,
            string correlationId, Reference user, IDictionary<string, string> properties,
            string parentJobId = null, int? itemCount = null) =>
            new JobCreateModel
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

        private async Task<Job> CreateJob(JobCreateModel createModel)
        {
            try
            {
                var job = await _jobClientResiliencePolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(createModel));
                
                GuardAgainstNullJob(job, createModel);

                return job;
            }
            catch (Exception ex)
            {
                _logger.Error( $"Failed to create job of type '{createModel.JobDefinitionId}' on specification '{createModel.Trigger.EntityId}'. {ex}");
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