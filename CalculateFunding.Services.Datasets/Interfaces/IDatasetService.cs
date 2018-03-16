using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetService
    {
        Task<IActionResult> CreateNewDataset(HttpRequest request);

        Task<IActionResult> GetDatasetByName(HttpRequest request);

        Task<IActionResult> ValidateDataset(HttpRequest request);

	    Task ProcessDataset(EventData message);

        Task<IActionResult> GetDatasetsByDefinitionId(HttpRequest request);
    }
}
