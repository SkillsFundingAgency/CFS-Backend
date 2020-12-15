using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProviderSourceDatasetBulkRepository
    {
        Task DeleteCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets);
        Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets);
        Task UpdateProviderSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets);
    }
}