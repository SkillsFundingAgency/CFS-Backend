using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsReportService
    {
        IActionResult GetReportMetadata(string specificationId, string targetFundingPeriodId = null);

        Task<IActionResult> DownloadReport(string reportId);
    }
}
