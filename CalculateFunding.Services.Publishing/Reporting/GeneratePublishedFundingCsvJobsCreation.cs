using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class GeneratePublishedFundingCsvJobsCreation : IGeneratePublishedFundingCsvJobsCreation
    {
        private readonly ICreateGeneratePublishedFundingCsvJobs _jobs;

        public GeneratePublishedFundingCsvJobsCreation(ICreateGeneratePublishedFundingCsvJobs jobs)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            
            _jobs = jobs;
        }

        public async Task CreateJobs(string specificationId, string correlationId, Reference user)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(user, nameof(user));
            
            await CreateJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentState);
            await CreateJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.Released);
            await CreateJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.History);
        }

        private Task<Job> CreateJob(string specification, string correlationId, Reference user, FundingLineCsvGeneratorJobType jobType)
        {
            return _jobs.CreateJob(specification, user, correlationId, JobTypeProperties(jobType));
        }

        private Dictionary<string, string> JobTypeProperties(FundingLineCsvGeneratorJobType jobType)
        {
            return new Dictionary<string, string>
            {
                {"job-type", jobType.ToString()}
            };
        }
    }
}