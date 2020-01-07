using CalculateFunding.Models.Datasets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IProviderSourceDatasetsRepository
    {
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdsAndSpecificationId(IEnumerable<string> providerIds, string specificationId);
    }
}   

