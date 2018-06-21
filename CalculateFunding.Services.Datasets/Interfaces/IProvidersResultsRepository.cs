using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProvidersResultsRepository
    {
        Task UpdateCurrentSourceDatsets(IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets, string specificationId);

        Task UpdateSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets, string specificationId);

        Task<IEnumerable<string>> GetAllProviderIdsForSpecificationid(string specificationId);
    }
}
