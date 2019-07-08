using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPoliciesRepository
    {
        Task<Period> GetFundingPeriodById(string fundingPeriodId);
        Task<IEnumerable<FundingStream>> GetFundingStreams();
    }
}