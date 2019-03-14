using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Jobs.Interfaces;
using Newtonsoft.Json.Linq;

namespace CalculateFunding.Services.Jobs.Repositories
{
    public class JobRepository : IJobRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public JobRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) cosmosHealth = await _cosmosRepository.IsHealthOk();

            health.Name = nameof(JobDefinitionsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = this.GetType().Name, Message = cosmosHealth.Message });

            return health;
        }

        public async Task<Job> CreateJob(Job job)
        {
            job.Created = DateTimeOffset.UtcNow;
            job.RunningStatus = RunningStatus.Queued;
            job.Id = Guid.NewGuid().ToString();
            job.LastUpdated = DateTimeOffset.UtcNow;

            HttpStatusCode result = await _cosmosRepository.CreateAsync(job);

            if (!result.IsSuccess())
            {
                throw new Exception($"Failed to save new job to cosmos with status code: {(int)result}");
            }

            return job;
        }

        public async Task<HttpStatusCode> CreateJobLog(JobLog jobLog)
        {
            return await _cosmosRepository.CreateAsync(jobLog);
        }

        public IQueryable<Job> GetJobs()
        {
            return _cosmosRepository.Query<Job>();
        }

        public async Task<Job> GetJobById(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            DocumentEntity<Job> job = await _cosmosRepository.ReadAsync<Job>(jobId);

            if (job == null)
            {
                return null;
            }

            return job.Content;
        }

        public IEnumerable<Job> GetRunningJobsForSpecificationAndJobDefinitionId(string specificationId, string jobDefinitionId)
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>().Where(m => m.SpecificationId == specificationId && m.JobDefinitionId == jobDefinitionId && m.RunningStatus != RunningStatus.Completed);

            return query.AsEnumerable();
        }

        public IEnumerable<JobLog> GetJobLogsByJobId(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            IQueryable<JobLog> jobLogs = _cosmosRepository.Query<JobLog>().Where(m => m.JobId == jobId);

            return jobLogs.AsEnumerable();
        }

        public async Task<HttpStatusCode> UpdateJob(Job job)
        {
            job.LastUpdated = DateTimeOffset.UtcNow;

            return await _cosmosRepository.UpsertAsync<Job>(job);
        }

        public IEnumerable<Job> GetChildJobsForParent(string jobId)
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>().Where(m => m.ParentJobId == jobId);

            return query.AsEnumerable();
        }

        public IEnumerable<Job> GetNonCompletedJobs()
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>().Where(m => !m.CompletionStatus.HasValue);

            return query.AsEnumerable();
        }

        public async Task<Job> GetLastestJobBySpecificationId(string specificationId, IEnumerable<string> jobDefinitionIds = null)
        {
            string query = $"select top 1 r.content.id as id, " +
                           "r.content.jobDefinitionId as jobDefinitionId, " +
                           "r.content.runningStatus as runningStatus, " +
                           "r.content.completionStatus as completionStatus, " +
                           "r.content.invokerUserId as invokeUserId, " +
                           "r.content.invokerDisplayName as invokerDisplayName, " +
                           "r.content.itemCount as itemCount, " +
                           "r.content.specificationId as specificationId, " +
                           "r.content.trigger.message as triggerMessage, " +
                           "r.content.trigger.entityId as triggerEntityId, " +
                           "r.content.trigger.entityType as triggerEntityType, " +
                           "r.content.parentJobId as parentJobId, " +
                           "r.content.supersededByJobId as supersededByJobId, " +
                           "r.content.correlationId as correlationId, " +
                           "r.content.properties as properties, " +
                           "r.content.messageBody as messageBody, " +
                           "r.content.created as created, " +
                           "r.content.completed as completed, " +
                           "r.content.outcome as outcome, " +
                           "r.content.lastUpdated as lastUpdated " +
                           "from r " +
                           "where r.documentType = 'Job' " +
                           "and r.deleted = false " +
                           "and r.content.specificationId = '" + specificationId + "' ";

            if (!jobDefinitionIds.IsNullOrEmpty())
            {
                IList<string> jobDefinitionIdFilters = new List<string>();

                foreach (string jobDefinitionId in jobDefinitionIds)
                {
                    jobDefinitionIdFilters.Add($"r.content.jobDefinitionId = '{jobDefinitionId}'");
                }

                query += $" and ({string.Join(" or ", jobDefinitionIdFilters)})";
            }

            query += " order by r.content.created desc";

            IEnumerable<dynamic> existingResults = await _cosmosRepository.QueryDynamic<dynamic>(query, true, 1);

            dynamic existingResult = existingResults.FirstOrDefault();

            if (existingResult == null)
            {
                return null;
            }

            return new Job
            {
                Id = existingResult.id,
                JobDefinitionId = existingResult.jobDefinitionId,
                RunningStatus = Enum.Parse(typeof(RunningStatus), existingResult.runningStatus),
                CompletionStatus = string.IsNullOrWhiteSpace(existingResult.completionStatus) ? null : Enum.Parse(typeof(CompletionStatus), existingResult.completionStatus),
                InvokerUserId = existingResult.invokeUserId,
                InvokerUserDisplayName = existingResult.invokerDisplayName,
                ItemCount = existingResult.itemCount,
                SpecificationId = existingResult.specificationId,
                Trigger = new Trigger
                {
                    Message = existingResult.triggerMessage != null ? existingResult.triggerMessage : "",
                    EntityId = existingResult.triggerEntityId != null ? existingResult.triggerEntityId : "",
                    EntityType = existingResult.triggerEntityType != null ? existingResult.triggerEntityType : ""
                },
                ParentJobId = existingResult.parentJobId,
                SupersededByJobId = existingResult.supersededByJobId,
                CorrelationId = existingResult.correlationId,
                Properties = existingResult.properties == null ? new Dictionary<string, string>() : ((JObject)existingResult.properties).ToObject<Dictionary<string, string>>(),
                MessageBody = existingResult.messageBody,
                Created = (DateTimeOffset)existingResult.created,
                Completed = (DateTimeOffset?)existingResult.completed,
                Outcome = existingResult.outcome,
                LastUpdated = (DateTimeOffset)existingResult.lastUpdated
            };
        }
    }
}