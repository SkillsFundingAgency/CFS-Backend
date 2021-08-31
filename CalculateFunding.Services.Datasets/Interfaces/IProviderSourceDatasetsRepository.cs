using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;


namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProviderSourceDatasetsRepository
    {
        Task DeleteCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets);

        Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets);

        Task UpdateProviderSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets);

        Task<IEnumerable<ProviderSourceDatasetHistory>> GetProviderSourceDatasetHistories(string specificationId, string relationshipId);

        Task<IEnumerable<DocumentEntity<ProviderSourceDataset>>> GetCurrentProviderSourceDatasets(string specificationId, string relationshipId);
    }
}
