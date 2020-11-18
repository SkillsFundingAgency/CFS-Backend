using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderUpdateDateService
    {
        Task<IActionResult> GetLatestPublishedDate(string fundingStreamId,
            string fundingPeriodId);
    }
}