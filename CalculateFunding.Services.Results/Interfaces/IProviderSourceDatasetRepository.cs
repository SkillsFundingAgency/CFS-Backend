using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderSourceDatasetRepository
    {
        Task<HttpStatusCode> UpsertProviderSourceDataset(ProviderSourceDataset providerSourceDataset);

        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId);
    }
}
