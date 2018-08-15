using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProvidersResultsRepository
    {
        Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets);

        Task UpdateProviderSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets);

        Task<IEnumerable<ProviderSourceDatasetHistory>> GetProviderSourceDatasetHistories(string specificationId, string relationshipId);

        Task<IEnumerable<ProviderSourceDatasetCurrent>> GetCurrentProviderSourceDatasets(string specificationId, string relationshipId);

        Task<IEnumerable<string>> GetAllProviderIdsForSpecificationid(string specificationId);
    }
}
