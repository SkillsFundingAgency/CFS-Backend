using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Jobs.Interfaces;

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
    }
}
