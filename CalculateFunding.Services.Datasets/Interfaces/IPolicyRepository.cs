using System.Collections.Generic;
using System.Threading.Tasks;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IPolicyRepository
    {
        Task<IEnumerable<PoliciesApiModels.FundingStream>> GetFundingStreams();
    }
}
