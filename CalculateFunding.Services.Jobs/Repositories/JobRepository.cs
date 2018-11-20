using CalculateFunding.Models.Jobs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Jobs.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Jobs.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public JobRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<Job> CreateJob(Job job)
        {
            throw new NotImplementedException();
        }

        public Task<JobLog> CreateJobLog(JobLog jobLog)
        {
            throw new NotImplementedException();
        }

        public Task<JobDefinition> GetJobDefinition(string jobType)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Job>> GetRunningJobsForSpecificationAndType(string specificationId, string jobType)
        {
            throw new NotImplementedException();
        }

        public Task<Job> UpdateJob(string jobId, Job job)
        {
            throw new NotImplementedException();
        }
    }
}
