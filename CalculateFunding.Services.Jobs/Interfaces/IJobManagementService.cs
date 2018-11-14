using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobManagementService
    {
        Task<IActionResult> CreateJob(JobCreateModel job, HttpRequest request);

        Task<IActionResult> AddJobLog(JobLogUpdateModel job, HttpRequest request);

        Task<IActionResult> CancelJob(string jobId, HttpRequest request);

        Task CancelJob(string jobId);


        Task TimeoutJob(string jobId);

        Task SupersedeJob(string existingJobId, string replacementJobId);
    }
}
