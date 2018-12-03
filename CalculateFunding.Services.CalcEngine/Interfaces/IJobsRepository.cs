using CalculateFunding.Models.Jobs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IJobsRepository
    {
        Task<JobLog> AddJobLog(string jobId, JobLogUpdateModel jobLogUpdateModel);

        Task<JobViewModel> GetJobById(string jobId);
    }
}
