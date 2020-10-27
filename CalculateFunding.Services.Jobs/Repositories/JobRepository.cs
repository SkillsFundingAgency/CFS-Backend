using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs.Interfaces;
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

        public Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            health.Name = nameof(JobDefinitionsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = this.GetType().Name, Message = Message });

            return Task.FromResult(health);
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

        public async Task<IEnumerable<Job>> GetJobs()
        {
            return await _cosmosRepository.Query<Job>();
        }

        public async Task<Job> GetJobById(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            DocumentEntity<Job> jobResult = await _cosmosRepository.TryReadDocumentByIdPartitionedAsync<Job>(jobId, jobId);

            if (jobResult != null && !jobResult.Deleted)
            {
                return jobResult.Content;
            }

            return null;
        }

        public async Task<IEnumerable<Job>> GetRunningJobsForSpecificationAndJobDefinitionId(string specificationId, string jobDefinitionId)
        {
            return await _cosmosRepository
                .Query<Job>((m) => m.Content.SpecificationId == specificationId
                            && m.Content.JobDefinitionId == jobDefinitionId
                            && m.Content.RunningStatus != RunningStatus.Completed);
        }

        public async Task<IEnumerable<JobLog>> GetJobLogsByJobId(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery("SELECT j FROM Jobs j WHERE j.documentType = \"JobLog\" AND j.deleted = false");

            IEnumerable<JobLog> jobLogs = await _cosmosRepository.QueryPartitionedEntity<JobLog>(cosmosDbQuery, partitionKey: jobId);

            return jobLogs;
        }

        public async Task<HttpStatusCode> UpdateJob(Job job)
        {
            job.LastUpdated = DateTimeOffset.UtcNow;

            return await _cosmosRepository.UpsertAsync<Job>(job, job.JobId);
        }

        public async Task<IEnumerable<Job>> GetChildJobsForParent(string jobId)
        {
            return (await _cosmosRepository.Query<Job>((m) => m.Content.ParentJobId == jobId));
        }

        public async Task<IEnumerable<Job>> GetNonCompletedJobs()
        {
            return (await _cosmosRepository.Query<Job>((m) =>
                m.Content.CompletionStatus != CompletionStatus.Cancelled &&
                m.Content.CompletionStatus != CompletionStatus.Failed &&
                m.Content.CompletionStatus != CompletionStatus.Succeeded &&
                m.Content.CompletionStatus != CompletionStatus.Superseded &&
                m.Content.CompletionStatus != CompletionStatus.TimedOut));
        }

        public async Task<Job> GetLatestJobBySpecificationIdAndDefinitionId(string specificationId, string jobDefinitionId)
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
                                    AND r.content.specificationId = @SpecificationId
                                    AND r.content.jobDefinitionId = @JobDefinitionId
                                    AND r.content.created > @Date
                                    ORDER BY r.content.created DESC";

            List<CosmosDbQueryParameter> cosmosDbQueryParameters = new List<CosmosDbQueryParameter>
            {
                new CosmosDbQueryParameter("@SpecificationId", specificationId),
                new CosmosDbQueryParameter("@JobDefinitionId", jobDefinitionId),
                new CosmosDbQueryParameter("@Date", DateTimeOffset.UtcNow.AddDays(-1))
            };

            IEnumerable<dynamic> latestJobResults = await _cosmosRepository.DynamicQuery(new CosmosDbQuery(query, cosmosDbQueryParameters));

            if (latestJobResults == null || latestJobResults.Count() == 0)
            {
                return null;
            }

            dynamic latestJob = latestJobResults.First();

            if (latestJob != null)
            {
                string runningStatus = latestJob.runningStatus;
                string completionStatus = latestJob.completionStatus;

                return new Job
                {
                    Id = latestJob.id,
                    JobDefinitionId = latestJob.jobDefinitionId,
                    RunningStatus = Enum.Parse<RunningStatus>(runningStatus),
                    CompletionStatus = string.IsNullOrWhiteSpace(completionStatus) ? default(CompletionStatus?) : Enum.Parse<CompletionStatus>(completionStatus),
                    InvokerUserId = latestJob.invokeUserId,
                    InvokerUserDisplayName = latestJob.invokerDisplayName,
                    ItemCount = latestJob.itemCount,
                    SpecificationId = latestJob.specificationId,
                    Trigger = new Trigger
                    {
                        Message = latestJob.triggerMessage ?? string.Empty,
                        EntityId = latestJob.triggerEntityId ?? string.Empty,
                        EntityType = latestJob.triggerEntityType ?? string.Empty
                    },
                    ParentJobId = latestJob.parentJobId,
                    SupersededByJobId = latestJob.supersededByJobId,
                    CorrelationId = latestJob.correlationId,
                    Properties = latestJob.properties == null ? new Dictionary<string, string>() : ((JObject)latestJob.properties).ToObject<Dictionary<string, string>>(),
                    MessageBody = latestJob.messageBody,
                    Created = (DateTimeOffset)latestJob.created,
                    Completed = (DateTimeOffset?)latestJob.completed,
                    Outcome = latestJob.outcome,
                    LastUpdated = (DateTimeOffset)latestJob.lastUpdated
                };
            }

            return null;
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

            List<CosmosDbQueryParameter> sqlParameters = new List<CosmosDbQueryParameter>
            {
                new CosmosDbQueryParameter($"@dateTimeFrom", dateTimeFrom),
                new CosmosDbQueryParameter($"@dateTimeTo", dateTimeTo),
            };

            query += " ORDER BY r.content.created DESC";

            IEnumerable<dynamic> existingResults = await _cosmosRepository.DynamicQuery(new CosmosDbQuery(query, sqlParameters), 1);

            IList<Job> jobs = new List<Job>();

            if (existingResults.IsNullOrEmpty())
            {
                return jobs;
            }

            foreach (dynamic existingResult in existingResults)
            {
                string runningStatus = existingResult.runningStatus;
                string completionStatus = existingResult.completionStatus;

                jobs.Add(new Job
                {
                    Id = existingResult.id,
                    JobDefinitionId = existingResult.jobDefinitionId,
                    RunningStatus = Enum.Parse<RunningStatus>(runningStatus),
                    CompletionStatus = string.IsNullOrWhiteSpace(completionStatus) ? default(CompletionStatus?) : Enum.Parse<CompletionStatus>(completionStatus),
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

        public async Task DeleteJobsBySpecificationId(string specificationId, DeletionType deletionType)
        {
            IEnumerable<Job> jobs = await GetJobsBySpecificationId(specificationId);

            List<Job> jobsList = jobs.ToList();

            if (!jobsList.Any())
                return;

            if (deletionType == DeletionType.SoftDelete)
                await _cosmosRepository.BulkDeleteAsync(jobsList.ToDictionary(c => c.Id), hardDelete: false);
            if (deletionType == DeletionType.PermanentDelete)
                await _cosmosRepository.BulkDeleteAsync(jobsList.ToDictionary(c => c.Id), hardDelete: true);
        }

        private async Task<IEnumerable<Job>> GetJobsBySpecificationId(string specificationId)
        {
            return await _cosmosRepository.Query<Job>(x => x.Content.SpecificationId == specificationId);
        }
    }
}