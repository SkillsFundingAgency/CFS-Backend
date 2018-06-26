using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderSourceDatasetRepository
    {
        Task<HttpStatusCode> UpsertProviderSourceDataset(ProviderSourceDatasetCurrent providerSourceDataset);

        Task<IEnumerable<ProviderSourceDatasetCurrent>> GetProviderSourceDatasets(string providerId, string specificationId);

        Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationId(string specificationId);
    }
}
