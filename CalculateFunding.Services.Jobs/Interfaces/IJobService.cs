using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobService
    {
        Task<IActionResult> GetJobById(string jobId, HttpRequest request);

        Task<IActionResult> GetJobDefinitions(HttpRequest request);

        Task<IActionResult> GetJobs(string specificationId, string jobType, string entityId, RunningStatus? runningStatus, CompletionStatus? completionStatus, int pageNumber, HttpRequest request);

        Task<IActionResult> GetJobLogs(string jobId, HttpRequest request);
    }
}
