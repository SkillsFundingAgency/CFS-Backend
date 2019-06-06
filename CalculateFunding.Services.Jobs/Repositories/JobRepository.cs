using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.Documents;
using Newtonsoft.Json.Linq;
using Trigger = CalculateFunding.Models.Jobs.Trigger;

namespace CalculateFunding.Services.Jobs.Repositories
{
    public class JobRepository : IJobRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public JobRepository(ICosmosRepository cosmosRepository)
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

            HttpStatusCode result = await _cosmosRepository.CreateAsync(job, job.JobId);

            if (!result.IsSuccess())
            {
                throw new Exception($"Failed to save new job to cosmos with status code: {(int)result}");
            }

            return job;
        }

        public async Task<HttpStatusCode> CreateJobLog(JobLog jobLog)
        {
            return await _cosmosRepository.CreateAsync(jobLog, jobLog.JobId);
        }

        public IQueryable<Job> GetJobs()
        {
            return _cosmosRepository.Query<Job>(enableCrossPartitionQuery: true);
        }

        public async Task<Job> GetJobById(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec("SELECT TOP 1 * FROM Jobs AS j WHERE j.documentType = \"Job\" AND j.deleted = false");

            IEnumerable<Job> jobs = await _cosmosRepository.QueryPartitionedEntity<Job>(sqlQuerySpec, partitionEntityId: jobId);

            return jobs.FirstOrDefault();
        }

        public IEnumerable<Job> GetRunningJobsForSpecificationAndJobDefinitionId(string specificationId, string jobDefinitionId)
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId && m.JobDefinitionId == jobDefinitionId && m.RunningStatus != RunningStatus.Completed);

            return query.AsEnumerable();
        }

        public async Task<IEnumerable<JobLog>> GetJobLogsByJobId(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec("SELECT j FROM Jobs j WHERE j.documentType = \"JobLog\" AND j.deleted = false");

            IEnumerable<JobLog> jobLogs = await _cosmosRepository.QueryPartitionedEntity<JobLog>(sqlQuerySpec, partitionEntityId: jobId);

            return jobLogs;
        }

        public async Task<HttpStatusCode> UpdateJob(Job job)
        {
            job.LastUpdated = DateTimeOffset.UtcNow;

            return await _cosmosRepository.UpsertAsync<Job>(job, job.JobId);
        }

        public IEnumerable<Job> GetChildJobsForParent(string jobId)
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>(enableCrossPartitionQuery: true).Where(m => m.ParentJobId == jobId);

            return query.AsEnumerable();
        }

        public IEnumerable<Job> GetNonCompletedJobs()
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>(enableCrossPartitionQuery: true).Where(m => !m.CompletionStatus.HasValue);

            return query.AsEnumerable();
        }

        public async Task<Job> GetLatestJobBySpecificationId(string specificationId, IEnumerable<string> jobDefinitionIds = null)
        {
            string query = @"SELECT TOP 1   r.content.id AS id, 
                                    r.content.jobDefinitionId AS jobDefinitionId, 
                                    r.content.runningStatus AS runningStatus, 
                                    r.content.completionStatus AS completionStatus, 
                                    r.content.invokerUserId AS invokeUserId, 
                                    r.content.invokerDisplayName AS invokerDisplayName, 
                                    r.content.itemCount AS itemCount, 
                                    r.content.specificationId AS specificationId, 
                                    r.content.trigger.message AS triggerMessage, 
                                    r.content.trigger.entityId AS triggerEntityId, 
                                    r.content.trigger.entityType AS triggerEntityType, 
                                    r.content.parentJobId AS parentJobId, 
                                    r.content.supersededByJobId AS supersededByJobId, 
                                    r.content.correlationId AS correlationId, 
                                    r.content.properties AS properties, 
                                    r.content.messageBody AS messageBody, 
                                    r.content.created AS created, 
                                    r.content.completed AS completed, 
                                    r.content.outcome AS outcome, 
                                    r.content.lastUpdated AS lastUpdated 
                            FROM    r 
                            WHERE   r.documentType = 'Job' 
                                    AND r.deleted = false 
                                    AND r.content.specificationId = @SpecificationId";

            List<SqlParameter> sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter("@SpecificationId", specificationId));

            string[] jobDefinitionIdsArray = jobDefinitionIds.ToArray();

            if (!jobDefinitionIds.IsNullOrEmpty())
            {
                IList<string> jobDefinitionIdFilters = new List<string>();

                for (int cnt = 0; cnt < jobDefinitionIds.Count(); cnt++)
                {
                    jobDefinitionIdFilters.Add($"r.content.jobDefinitionId = @JobDefinitionId{cnt}");
                    sqlParameters.Add(new SqlParameter($"@JobDefinitionId{cnt}", jobDefinitionIdsArray[cnt]));
                }
                query += $" AND ({string.Join(" or ", jobDefinitionIdFilters)})";
            }

            query += " ORDER BY r.content.created DESC";

            IEnumerable<dynamic> existingResults = await _cosmosRepository.QueryDynamic(new SqlQuerySpec(query, new SqlParameterCollection(sqlParameters)), true, 1);

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
                    Message = existingResult.triggerMessage ?? string.Empty,
                    EntityId = existingResult.triggerEntityId ?? string.Empty,
                    EntityType = existingResult.triggerEntityType ?? string.Empty
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

        public async Task<IEnumerable<Job>> GetRunningJobsWithinTimeFrame(string dateTimeFrom, string dateTimeTo)
        {
            string query = @"SELECT r.content.id AS id, 
                                    r.content.jobDefinitionId AS jobDefinitionId, 
                                    r.content.runningStatus AS runningStatus, 
                                    r.content.completionStatus AS completionStatus, 
                                    r.content.invokerUserId AS invokeUserId, 
                                    r.content.invokerDisplayName AS invokerDisplayName, 
                                    r.content.itemCount AS itemCount, 
                                    r.content.specificationId AS specificationId, 
                                    r.content.trigger.message AS triggerMessage, 
                                    r.content.trigger.entityId AS triggerEntityId, 
                                    r.content.trigger.entityType AS triggerEntityType, 
                                    r.content.parentJobId AS parentJobId, 
                                    r.content.supersededByJobId AS supersededByJobId, 
                                    r.content.correlationId AS correlationId, 
                                    r.content.properties AS properties, 
                                    r.content.messageBody AS messageBody, 
                                    r.content.created AS created, 
                                    r.content.completed AS completed, 
                                    r.content.outcome AS outcome, 
                                    r.content.lastUpdated AS lastUpdated 
                            FROM    r 
                            WHERE   r.documentType = 'Job' 
                                    AND r.deleted = false 
                                    AND (r.content.lastUpdated >= @dateTimeFrom and r.content.lastUpdated <= @dateTimeTo) and r.content.runningStatus != 'Completed'";

            List<SqlParameter> sqlParameters = new List<SqlParameter>
            {
                new SqlParameter($"@dateTimeFrom", dateTimeFrom),
                new SqlParameter($"@dateTimeTo", dateTimeTo),
            };

            query += " ORDER BY r.content.created DESC";

            IEnumerable<dynamic> existingResults = await _cosmosRepository.QueryDynamic(new SqlQuerySpec(query, new SqlParameterCollection(sqlParameters)), true, 1);

            IList<Job> jobs = new List<Job>();

            if (existingResults.IsNullOrEmpty())
            {
                return jobs;
            }

            foreach (dynamic existingResult in existingResults)
            {
                jobs.Add(new Job
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
                        Message = existingResult.triggerMessage ?? string.Empty,
                        EntityId = existingResult.triggerEntityId ?? string.Empty,
                        EntityType = existingResult.triggerEntityType ?? string.Empty
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
                });
            }

            return jobs;
        }
    }
}