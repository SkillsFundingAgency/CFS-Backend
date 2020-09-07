using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Jobs.Controllers
{
    public class JobsController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly IJobManagementService _jobManagementService;
        private readonly IJobDefinitionsService _jobDefinitionsService;

        public JobsController(IJobService jobService,
            IJobManagementService jobManagementService,
            IJobDefinitionsService jobDefinitionsService)
        {
            _jobService = jobService;
            _jobManagementService = jobManagementService;
            _jobDefinitionsService = jobDefinitionsService;
        }

        [HttpGet]
        [Route("api/jobs/{jobId}/{includeChildJobs?}")]
        [ProducesResponseType(200, Type = typeof(JobCurrentModel))]
        public async Task<IActionResult> GetJobById([FromRoute] string jobId,
            bool includeChildJobs = false) =>
            await _jobService.GetJobById(jobId, includeChildJobs);

        [HttpPut]
        [Route("api/jobs/{jobId}")]
        [ProducesResponseType(200, Type = typeof(JobSummary))]
        public async Task<IActionResult> UpdateJob(string jobId,
            JobUpdateModel jobUpdate) =>
            await _jobService.UpdateJob(jobId, jobUpdate);

        /// <summary>
        ///     Safe create single job end point.
        ///     will return error status of single job creation
        ///     in event of failure
        /// </summary>
        /// <param name="job">details of the job to create</param>
        /// <returns>details of the job creation call including any error information in the event of failure</returns>
        [HttpPost("api/jobs/try-create-job")]
        [ProducesResponseType(200, Type = typeof(JobSummary))]
        public async Task<IActionResult> TryCreateJob([FromBody] JobCreateModel job)
            => await _jobManagementService.TryCreateJobs(new [] { job }, CurrentUserOrDefault);

        /// <summary>
        ///     Safe create single job end point.
        ///     will return error status of single job creation
        ///     in event of failure
        /// </summary>
        /// <param name="jobs">details of the jobs to create</param>
        /// <returns>details of the each individual job creation call including any error information in the event of failure</returns>
        [HttpPost("api/jobs/try-create-jobs")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<JobSummary>))]
        public async Task<IActionResult> TryCreateJobs([FromBody] IEnumerable<JobCreateModel> jobs)
            => await _jobManagementService.TryCreateJobs(jobs, CurrentUserOrDefault);

        [HttpPost]
        [Route("api/jobs")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<JobSummary>))]
        public async Task<IActionResult> CreateJobs([FromBody] IEnumerable<JobCreateModel> jobs) => await _jobManagementService.CreateJobs(jobs, CurrentUserOrDefault);

        [HttpGet]
        [Route("api/jobs/jobdefinitions/{jobDefinitionId}")]
        [ProducesResponseType(200, Type = typeof(JobDefinition))]
        public async Task<IActionResult> GetJobDefinitionById(string jobDefinitionId) => await _jobDefinitionsService.GetJobDefinitionById(jobDefinitionId);

        [HttpGet]
        [Route("api/jobs/jobdefinitions")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<JobDefinition>))]
        public async Task<IActionResult> GetJobDefinitions() => await _jobDefinitionsService.GetJobDefinitions();

        [HttpPost]
        [Route("api/jobs/jobdefinitions")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> SaveJobDefinition([FromBody] JobDefinition jobDefinition) => await _jobDefinitionsService.SaveDefinition(jobDefinition);

        [HttpGet]
        [Route("api/jobs")]
        [ProducesResponseType(200, Type = typeof(JobQueryResponseModel))]
        public async Task<IActionResult> GetJobs([FromQuery] string specificationId = null,
            [FromQuery] string jobType = null,
            [FromQuery] string entityId = null,
            [FromQuery] RunningStatus? runningStatus = null,
            [FromQuery] CompletionStatus? completionStatus = null,
            [FromQuery] bool excludeChildJobs = false,
            [FromQuery] int pageNumber = 1) =>
            await _jobService.GetJobs(specificationId, jobType, entityId, runningStatus, completionStatus, excludeChildJobs, pageNumber);

        [HttpGet]
        [Route("api/jobs/latest")]
        [ProducesResponseType(200, Type = typeof(JobSummary))]
        public async Task<IActionResult> GetLatestJobs([FromQuery] string specificationId, [FromQuery] string jobTypes)
        {
            return await _jobService.GetLatestJobs(specificationId, jobTypes);
        }


        [HttpGet]
        [Route("api/jobs/{jobId}/logs")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<JobLog>))]
        public async Task<IActionResult> GetJobLogs([FromRoute] string jobId) => await _jobService.GetJobLogs(jobId);

        [HttpPost]
        [Route("api/jobs/{jobId}/logs")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpdateJobStatusLog([FromRoute] string jobId,
            [FromBody] JobLogUpdateModel job) =>
            await _jobManagementService.AddJobLog(jobId, job);

        /// <summary>
        ///     Manual user cancellation of job
        ///     Not needed in first phase
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/jobs/{jobId}/cancel")]
        [ProducesResponseType(202)]
        public async Task<IActionResult> CancelJob(string jobId) => await _jobManagementService.CancelJob(jobId);

        [HttpGet]
        [Route("api/jobs/noncompleted/dateTimeFrom/{dateTimeFrom}/dateTimeTo/{dateTimeTo}")]
        [ProducesResponseType(200, Type = typeof(JobSummary))]
        public async Task<IActionResult> GetCreatedJobsWithinTimeFrame([FromQuery] DateTimeOffset dateTimeFrom,
            [FromQuery] DateTimeOffset dateTimeTo) =>
            await _jobService.GetCreatedJobsWithinTimeFrame(dateTimeFrom, dateTimeTo);

        private Reference CurrentUserOrDefault => Request.GetUserOrDefault();
    }
}