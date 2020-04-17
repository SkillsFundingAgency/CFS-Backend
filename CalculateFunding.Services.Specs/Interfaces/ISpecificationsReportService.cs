using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsReportService
    {
        IActionResult GetReportMetadata(string specificationId);

        Task<IActionResult> DownloadReport(SpecificationReportIdentifier id);
    }
}
