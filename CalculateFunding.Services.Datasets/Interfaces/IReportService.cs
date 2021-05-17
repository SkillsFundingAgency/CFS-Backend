using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IReportService
    {
        IActionResult GetReportMetadata(string specificationId);
    }
}
