using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    public class SqlImportController : ControllerBase
    {
        private readonly ISqlImporter _importer;
        private readonly ISqlImportService _importService;

        public SqlImportController(ISqlImporter importer, ISqlImportService importService)
        {
            Guard.ArgumentNotNull(importer, nameof(importer));
            Guard.ArgumentNotNull(importService, nameof(importService));
            
            _importer = importer;
            _importService = importService;
        }

        [HttpGet("api/sqlqa/specifications/{specificationId}/funding-streams/{fundingStreamId}/import/queue")]
        public async Task<IActionResult> QueueImportDataJob([FromRoute] string specificationId,
            [FromRoute] string fundingStreamId)
            => await _importService.QueueSqlImport(specificationId, 
                fundingStreamId, 
                Request.GetUser(), 
                Request.GetCorrelationId());
    }
}