using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsReportService
    {
        IActionResult GetReportMetadata(string specificationId);

        IActionResult DownloadReport(string fileName, ReportType reportType);
    }
}
