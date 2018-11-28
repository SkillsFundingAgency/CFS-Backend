using CalculateFunding.Models.Jobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IJobsRepository
    {
        Task<Job> CreateJob(JobCreateModel jobCreateModel);
    }
}
