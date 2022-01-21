using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    public class SqlExportController : ControllerBase
    {
        readonly IQaSchemaService _schemaService;

        public SqlExportController(IQaSchemaService schemaService)
        {
            Guard.ArgumentNotNull(schemaService, nameof(schemaService));

            _schemaService = schemaService;
        }

        [HttpGet("api/sqlqa/specifications/{specificationId}/ensure-schema")]
        public async Task<IActionResult> EnsureSchemaForSpecification([FromRoute] string specificationId)
        {
            await _schemaService.EnsureSqlTablesForSpecification(specificationId, SqlExportSource.CurrentPublishedProviderVersion);

            return NoContent();
        }
    }
}