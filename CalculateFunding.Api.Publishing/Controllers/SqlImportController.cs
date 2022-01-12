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
        private readonly ISqlImportService _importService;
        private readonly IReleasedSqlImportService _releasedImportService;

        public SqlImportController(IReleasedSqlImportService releasedImportService, ISqlImportService importService)
        {
            Guard.ArgumentNotNull(releasedImportService, nameof(releasedImportService));
            Guard.ArgumentNotNull(importService, nameof(importService));
            
            _importService = importService;
            _releasedImportService = releasedImportService;
        }

        [HttpGet("api/sqlqa/specifications/{specificationId}/funding-streams/{fundingStreamId}/import/queue")]
        public async Task<IActionResult> QueueImportDataJob([FromRoute] string specificationId,
            [FromRoute] string fundingStreamId)
            => await _importService.QueueSqlImport(specificationId, 
                fundingStreamId, 
                Request.GetUser(), 
                Request.GetCorrelationId());

        [HttpGet("api/sqlqa/specifications/{specificationId}/funding-streams/{fundingStreamId}/released/import/queue")]
        public async Task<IActionResult> QueueReleasedImportDataJob([FromRoute] string specificationId,
            [FromRoute] string fundingStreamId)
            => await _releasedImportService.QueueSqlImport(specificationId,
                fundingStreamId,
                Request.GetUser(),
                Request.GetCorrelationId());
    }
}