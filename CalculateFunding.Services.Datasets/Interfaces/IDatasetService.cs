using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetService : IJobProcessingService
    {
        Task<IActionResult> CreateNewDataset(CreateNewDatasetModel model, Reference author);

        Task<IActionResult> DatasetVersionUpdate(DatasetVersionUpdateModel model, Reference author);

        Task<IActionResult> GetDatasetByDatasetId(string datasetId);

        Task<IActionResult> GetDatasetByName(HttpRequest request);

        Task<IActionResult> GetCurrentDatasetVersionByDatasetId(string datasetId);

        Task<IActionResult> ValidateDataset(GetDatasetBlobModel getDatasetBlobModel, Reference user, string correlationId);

        Task<IActionResult> GetDatasetsByDefinitionId(string definitionId);

        Task<IActionResult> DownloadDatasetFile(string datasetId, string datasetVersion);

        Task<IActionResult> UploadDatasetFile(string filename, DatasetMetadataViewModel datasetMetadataViewModel);

        Task<IActionResult> Reindex();

	    Task<IActionResult> ReindexDatasetVersions();

        Task<IActionResult> RegenerateProviderSourceDatasets(string specificationId, Reference user, string correlationId);

        Task<IActionResult> GetValidateDatasetStatus(string operationId);

        Task UpdateDatasetAndVersionDefinitionName(Reference datsetDefinitionReference);

        Task DeleteDatasets(Message message);
    }
}
