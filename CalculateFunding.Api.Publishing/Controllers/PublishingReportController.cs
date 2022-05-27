using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishingReportController : ControllerBase
    {
        private readonly IPublishedFundingCsvJobsService _publishFundingCsvJobsService;

        public PublishingReportController(
                    IPublishedFundingCsvJobsService publishFundingCsvJobsService)
        {
            _publishFundingCsvJobsService = publishFundingCsvJobsService;
        }

        /// <summary>
        /// Queue report jobs for target specification for selected action
        /// </summary>
        /// <param name="createAction">The type of report jobs to queue Refresh, Approve or Release</param>
        /// <param name="specificationId">Target specification to queue reporting jobs from</param>
        /// <returns></returns>
        [HttpGet("api/specifications/{createAction}/{specificationId}/queue-report-jobs")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Job>))]
        public async Task<IActionResult> QueueReportJobs(
            [FromRoute] GeneratePublishingCsvJobsCreationAction createAction,
            [FromRoute] string specificationId)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();
            
            return new OkObjectResult(await _publishFundingCsvJobsService.QueueCsvJobs(createAction,
                specificationId,
                correlationId,
                user));
        }
    }
}
