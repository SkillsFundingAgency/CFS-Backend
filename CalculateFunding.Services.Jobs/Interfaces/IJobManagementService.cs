using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobManagementService : IProcessingService
    {
        Task<IActionResult> CreateJobs(IEnumerable<JobCreateModel> jobs, Reference user);

        Task<IActionResult> AddJobLog(string jobId, JobLogUpdateModel jobLogUpdateModel);

        Task<IActionResult> CancelJob(string jobId);

        Task SupersedeJob(Job runningJob, Job replacementJob);

        Task CheckAndProcessTimedOutJobs();

        Task<IActionResult> TryCreateJobs(IEnumerable<JobCreateModel> jobs, Reference user);
    }
}
