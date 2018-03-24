using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{

    public interface IProviderRepository
    {
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string providerId, string specificationId);

        Task<HttpStatusCode> UpdateProviderSourceDataset(ProviderSourceDataset providerSourceDataset);

        Task<IEnumerable<ProviderSummary>> GetAllProviderSummaries();
    }
}

