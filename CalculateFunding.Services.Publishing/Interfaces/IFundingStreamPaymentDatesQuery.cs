using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingStreamPaymentDatesQuery
    {
        Task<IActionResult> GetFundingStreamPaymentDates(string fundingStreamId, string fundingPeriodId);
    }
}