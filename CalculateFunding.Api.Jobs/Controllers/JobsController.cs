using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Jobs.Controllers
{
    public class JobsController : Controller
    {
        private readonly IJobService _jobService;
        private readonly IJobManagementService _managementService;

        public JobsController(IJobService jobService, IJobManagementService jobManagementService)
        {
            _jobService = jobService;
            _managementService = jobManagementService;
        }

        [HttpGet]
        [Route("api/jobs/{jobId}")]
        [ProducesResponseType(200, Type = typeof(Job))]
        public async Task<IActionResult> GetJobById(string jobId)
        {
            return await _jobService.GetJobById(jobId, ControllerContext.HttpContext.Request);
        }

        [HttpPost]
        [Route("api/jobs")]
        [ProducesResponseType(201, Type = typeof(Job))]
        public async Task<IActionResult> CreateJob(JobCreateModel job)
        {
            return await _managementService.CreateJob(job, ControllerContext.HttpContext.Request);
        }

        [HttpGet]
        [Route("api/jobsdefinitions")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<JobType>))]
        public async Task<IActionResult> GetJobDefinitions()
        {
            return await _jobService.GetJobDefinitions(ControllerContext.HttpContext.Request);
        }

        [HttpGet]
        [Route("api/jobs")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Job>))]
        public async Task<IActionResult> GetJobs(
                                                    [FromQuery] string specificationId = null,
                                                    [FromQuery] string jobType = null,
                                                    [FromQuery] string entityId = null,
                                                    [FromQuery] RunningStatus? runningStatus = null,
                                                    [FromQuery] CompletionStatus? completionStatus = null,
                                                    [FromQuery] int pageNumber = 1)
        {
            // Sorted by last updated DESC
            // Any combination of above filters, when a filter is null, all jobs for that filter is displayed
            return await _jobService.GetJobs(specificationId, jobType, entityId, runningStatus, completionStatus, pageNumber, ControllerContext.HttpContext.Request);
        }

        [HttpGet]
        [Route("api/jobs/{jobId}/logs")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<JobType>))]
        public async Task<IActionResult> GetJobLogs([FromRoute] string jobId)
        {
            return await _jobService.GetJobLogs(jobId, ControllerContext.HttpContext.Request);
        }

        [HttpPost]
        [Route("api/jobs/{jobId}/log")]
        [ProducesResponseType(202)]
        public async Task<IActionResult> UpdateJobStatusLog(JobLogUpdateModel job)
        {
            return await _managementService.AddJobLog(job, ControllerContext.HttpContext.Request);
        }

        [HttpPost]
        [Route("api/jobs/{jobId}/cancel")]
        [ProducesResponseType(202)]
        public async Task<IActionResult> CancelJob(string jobId)
        {
            return await _managementService.CancelJob(jobId, ControllerContext.HttpContext.Request);
        }
    }
}
