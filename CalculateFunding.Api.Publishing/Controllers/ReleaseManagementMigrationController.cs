using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class ReleaseManagementMigrationController : ControllerBase
    {
        private readonly IPublishingV3ToSqlMigrator _migrator;

        public ReleaseManagementMigrationController(IPublishingV3ToSqlMigrator publishingV3ToSqlMigrator)
        {
            _migrator = publishingV3ToSqlMigrator;
        }

        [HttpGet("api/releasemanagement/queuereleasemanagementdatamigrationjob")]
        public async Task<IActionResult> QueueReleaseManagementDataMigrationJob([FromQuery] string[] fundingStreamIds, [FromQuery] string fundingPeriodId)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUserOrDefault();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();
            
            return await _migrator.QueueReleaseManagementDataMigrationJob(user, correlationId, fundingStreamIds, fundingPeriodId);
        }
    }
}
