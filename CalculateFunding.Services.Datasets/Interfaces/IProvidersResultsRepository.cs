using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProvidersResultsRepository
    {
        Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets, string specificationId);

        Task UpdateProviderSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets, string specificationId);

        Task<IEnumerable<ProviderSourceDatasetHistory>> GetProviderSourceDatasetHistories(string specificationId, string relationshipId);

        Task<IEnumerable<ProviderSourceDatasetCurrent>> GetCurrentProviderSourceDatasets(string specificationId, string relationshipId);

        Task<IEnumerable<string>> GetAllProviderIdsForSpecificationid(string specificationId);
    }
}
