using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProvidersResultsRepository
    {
        Task UpdateSourceDatsets(IEnumerable<ProviderSourceDataset> providerSourceDatasets, string specificationId);

        Task<IEnumerable<string>> GetAllProviderIdsForSpecificationid(string specificationId);
    }
}
