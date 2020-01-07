using CalculateFunding.Models.Calcs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task<ProviderResult> GetProviderResultByProviderIdAndSpecificationId(string providerId, string specificationId);
    }
}   

