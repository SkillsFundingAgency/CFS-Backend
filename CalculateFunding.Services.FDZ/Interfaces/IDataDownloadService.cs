using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.FDZ.Interfaces
{
    public interface IDataDownloadService
    {
        Task<IActionResult> GetDataForDataset(string datasetCode, int version);
    }
}