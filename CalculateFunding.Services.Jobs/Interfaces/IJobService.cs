using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobService
    {
        Task<IActionResult> GetJobById(string jobId, bool includeChildJobs);

        IActionResult GetJobs(string specificationId, string jobType, string entityId, RunningStatus? runningStatus, CompletionStatus? completionStatus, bool excludeChildJobs, int pageNumber);

        Task<IActionResult> GetJobLogs(string jobId);

        Task<IActionResult> UpdateJob(string jobId, JobUpdateModel jobUpdate, HttpRequest request);
    }
}
