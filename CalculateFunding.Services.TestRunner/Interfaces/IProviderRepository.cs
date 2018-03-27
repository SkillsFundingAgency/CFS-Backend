using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IProviderRepository
    {
        Task<ProviderResult> GetProviderById(string providerId, string specificationId);
    }
}
