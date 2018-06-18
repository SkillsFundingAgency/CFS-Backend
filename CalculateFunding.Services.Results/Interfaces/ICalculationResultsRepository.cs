using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ICalculationResultsRepository
    {
	    Task<ProviderResult> GetProviderResult(string providerId, string specificationId);
        Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId);
        Task<HttpStatusCode> UpdateProviderResults(List<ProviderResult> results);
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxItemCount = -1);
        Task<IEnumerable<DocumentEntity<ProviderResult>>> GetAllProviderResults();
        Task<decimal> GetCalculationResultTotalForSpecificationId(string specificationId);
    }
}
