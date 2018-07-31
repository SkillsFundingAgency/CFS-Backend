using CalculateFunding.Models.Results;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderProfilingRepository
    {
        Task<ProviderProfilingResponseModel> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel);
    }
}
