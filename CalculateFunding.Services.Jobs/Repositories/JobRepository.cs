using CalculateFunding.Models.Health;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Jobs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
            job.Created = DateTimeOffset.Now;
            job.RunningStatus = RunningStatus.Queued;
            job.Id = Guid.NewGuid().ToString();

            HttpStatusCode result = await _cosmosRepository.CreateAsync(job);

            if (!result.IsSuccess())
            {
                throw new Exception($"Failed to save new job to cosmos with status code: {(int)result}");
            }

            return job;
        }

        public Task<JobLog> CreateJobLog(JobLog jobLog)
        {
            throw new NotImplementedException();
        }

        public Task<JobDefinition> GetJobDefinition(string jobType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Job> GetRunningJobsForSpecificationAndJobDefinitionId(string specificationId, string jobDefinitionId)
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>().Where(m => m.SpecificationId == specificationId && m.JobDefinitionId == jobDefinitionId && m.RunningStatus != RunningStatus.Completed);

            return query.AsEnumerable();
        }

        public async Task<HttpStatusCode> UpdateJob(Job job)
        {
            return await _cosmosRepository.UpsertAsync<Job>(job);
        }

        public Job GetJobById(string jobId)
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>().Where(m => m.Id == jobId);

            return query.SingleOrDefault();
        }

        public IEnumerable<Job> GetChildJobsForParent(string jobId)
        {
            IQueryable<Job> query = _cosmosRepository.Query<Job>().Where(m => m.ParentJobId == jobId);

            return query.AsEnumerable();
        }
    }
}
