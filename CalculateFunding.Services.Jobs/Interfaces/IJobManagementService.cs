using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobManagementService
    {
        Task<IActionResult> CreateJobs(IEnumerable<JobCreateModel> jobs, HttpRequest request);

        Task<IActionResult> AddJobLog(JobLogUpdateModel job, HttpRequest request);

        Task<IActionResult> CancelJob(string jobId, HttpRequest request);

        Task CancelJob(string jobId);

        Task TimeoutJob(string jobId);

        Task SupersedeJob(Job runningJob, string replacementJobId);
        
        Task ProcessJobCompletion(Message message);
    }
}
