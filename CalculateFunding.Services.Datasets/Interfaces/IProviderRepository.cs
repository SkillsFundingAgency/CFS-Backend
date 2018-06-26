using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{

    public interface IProviderRepository
    {
        Task<IEnumerable<ProviderSourceDatasetCurrent>> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string providerId, string specificationId);

        Task<HttpStatusCode> UpdateProviderSourceDataset(ProviderSourceDatasetCurrent providerSourceDataset);

        Task<IEnumerable<ProviderSummary>> GetAllProviderSummaries();
    }
}

