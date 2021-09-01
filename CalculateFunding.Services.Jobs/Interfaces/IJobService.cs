using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobService
    {
        Task<IActionResult> GetJobById(string jobId, bool includeChildJobs);

        Task<IActionResult> GetJobs(string specificationId, string jobType, string entityId, RunningStatus? runningStatus, CompletionStatus? completionStatus, bool excludeChildJobs, int pageNumber);

        Task<IActionResult> GetLatestJobs(string specificationId, IEnumerable<string> jobDefinitionIds);

        Task<IActionResult> GetLatestJobsByJobDefinitionIds(IEnumerable<string> jobDefinitionIds);

        Task<IActionResult> GetJobLogs(string jobId);

        Task<IActionResult> UpdateJob(string jobId, JobUpdateModel jobUpdate);

        Task<IActionResult> GetCreatedJobsWithinTimeFrame(DateTimeOffset dateTimeFrom, DateTimeOffset dateTimeTo);

        Task<IActionResult> GetLatestSuccessfulJob(string specificationId, string jobDefinitionId);

        Task<IActionResult> GetLatestJobByTriggerEntityId(string specificationId, string entityId);
    }
}
