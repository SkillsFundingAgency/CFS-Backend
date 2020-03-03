using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProfileTotalsService
    {
        Task<IActionResult> GetPaymentProfileTotalsForFundingStreamForProvider(string fundingStreamId, string fundingPeriodId, string providerId);

        Task<IActionResult> GetAllReleasedPaymentProfileTotalsForFundingStreamForProvider(string fundingStreamId, string fundingPeriodId, string providerId);
    }
}