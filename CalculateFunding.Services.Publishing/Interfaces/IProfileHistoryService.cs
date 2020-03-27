using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProfileHistoryService
    {
        Task<IActionResult> GetProfileHistory(string fundingStreamId, 
            string fundingPeriodId,
            string providerId);
    }
}