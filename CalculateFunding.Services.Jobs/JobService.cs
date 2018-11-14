using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs
{
    public class JobService : IJobService
    {
        public Task<IActionResult> GetJobById(string jobId, HttpRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> GetJobDefinitions(HttpRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> GetJobLogs(string jobId, HttpRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> GetJobs(string specificationId, string jobType, string entityId, RunningStatus? runningStatus, CompletionStatus? completionStatus, int pageNumber, HttpRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
