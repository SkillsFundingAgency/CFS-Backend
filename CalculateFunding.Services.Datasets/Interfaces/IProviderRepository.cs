using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{

    public interface IProviderRepository
    {
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string providerId, string specificationId);

        Task<IEnumerable<ProviderSummary>> GetAllProviderSummaries();
    }
}

