using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IPoliciesRepository
    {
        Task<IEnumerable<FundingStream>> GetFundingStreams();
    }
}