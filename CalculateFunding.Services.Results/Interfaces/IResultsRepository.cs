using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsRepository
    {
	    Task<ProviderResult> GetProviderResult(string providerId, string specificationId);
	    Task<List<ProviderResult>> GetSpecificationResults(string providerId);

		Task UpdateProviderResults(List<ProviderResult> results);
    }
}
