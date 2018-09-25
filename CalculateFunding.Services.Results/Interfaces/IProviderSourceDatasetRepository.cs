using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderSourceDatasetRepository
    {
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId);

        Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationId(string specificationId);
    }
}
