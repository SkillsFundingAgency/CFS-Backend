using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Datasets.Controllers
{
    /// <summary>
    /// Datasets Report end points
    /// </summary>
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            Guard.ArgumentNotNull(reportService, nameof(reportService));

            _reportService = reportService;
        }

        [Route("api/datasets/reports/{specificationId}/report-metadata")]
        [HttpGet]
        [Produces(typeof(DatasetDownloadModel))]
        public IActionResult DownloadConverterWizardReportFile(
            [FromRoute] string specificationId) =>
                _reportService.GetReportMetadata(specificationId);
    }
}
