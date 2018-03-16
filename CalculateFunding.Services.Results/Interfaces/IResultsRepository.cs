using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsRepository
    {
	    Task<ProviderResult> GetProviderResult(string providerId, string specificationId);
        Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId);
        Task<HttpStatusCode> UpdateProviderResults(List<ProviderResult> results);
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId);
        Task<HttpStatusCode> UpsertProviderSourceDataset(ProviderSourceDataset providerSourceDataset);
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId);
    }
}
