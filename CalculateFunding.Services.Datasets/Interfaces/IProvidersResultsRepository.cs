using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProvidersResultsRepository
    {
        Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets);

        Task UpdateProviderSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets);

        Task<IEnumerable<ProviderSourceDatasetHistory>> GetProviderSourceDatasetHistories(string specificationId, string relationshipId);

        Task<IEnumerable<ProviderSourceDataset>> GetCurrentProviderSourceDatasets(string specificationId, string relationshipId);
    }
}
