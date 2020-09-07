using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobService
    {
        Task<IActionResult> GetJobById(string jobId, bool includeChildJobs);

        Task<IActionResult> GetJobs(string specificationId, string jobType, string entityId, RunningStatus? runningStatus, CompletionStatus? completionStatus, bool excludeChildJobs, int pageNumber);

        Task<IActionResult> GetLatestJobs(string specificationId, string jobTypes);

        Task<IActionResult> GetJobLogs(string jobId);

        Task<IActionResult> UpdateJob(string jobId, JobUpdateModel jobUpdate);

        Task<IActionResult> GetCreatedJobsWithinTimeFrame(DateTimeOffset dateTimeFrom, DateTimeOffset dateTimeTo);
    }
}
