using CalculateFunding.Models.Results;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task<ProviderResult> GetProviderByIdAndSpecificationId(string providerId, string specificationId);
    }
}   

