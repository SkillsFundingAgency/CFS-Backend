using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetService
    {
        Task<IActionResult> CreateNewDataset(HttpRequest request);

        Task<IActionResult> DatasetVersionUpdate(HttpRequest request);

        Task<IActionResult> GetDatasetByName(HttpRequest request);

        Task<IActionResult> GetCurrentDatasetVersionByDatasetId(HttpRequest request);

        Task<IActionResult> ValidateDataset(HttpRequest request);

        Task ValidateDataset(Message message);

        Task<IActionResult> GetDatasetsByDefinitionId(HttpRequest request);

        Task<IActionResult> DownloadDatasetFile(HttpRequest request);

        Task<IActionResult> Reindex(HttpRequest request);

        Task<IActionResult> RegenerateProviderSourceDatasets(HttpRequest httpRequest);

        Task<IActionResult> GetValidateDatasetStatus(HttpRequest httpRequest);
    }
}
