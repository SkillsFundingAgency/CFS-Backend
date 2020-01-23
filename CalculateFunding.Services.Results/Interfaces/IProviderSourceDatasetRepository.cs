using CalculateFunding.Models.Datasets;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Messages;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderSourceDatasetRepository
    {
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId);

        Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationId(string specificationId);

        Task DeleteProviderSourceDataset(string providerSourceDatasetId, DeletionType deletionType);

        Task DeleteProviderSourceDatasetVersion(string providerSourceDatasetVersionId, DeletionType deletionType);
    }
}
