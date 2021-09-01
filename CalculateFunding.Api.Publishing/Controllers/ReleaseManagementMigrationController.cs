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

        [HttpPost("api/releasemanagement/populatereferencedata")]
        public async Task<IActionResult> PopulateReferenceData()
        {
            // TODO - migrate to job
            //await _migrator.PopulateReferenceData();

            return NoContent();
        }
    }
}
