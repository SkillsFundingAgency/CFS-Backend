using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IDataDownloadService
    {
        Task<IActionResult> GetDataForDataset(string datasetCode, int version);
    }
}