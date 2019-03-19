using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface IFundingService
    {
        Task<IActionResult> GetFundingStreams();

        Task<IActionResult> GetFundingStreamById(string fundingStreamId);

        Task<IActionResult> GetFundingStreamById(HttpRequest request);

        Task<IActionResult> SaveFundingStream(HttpRequest request);

        Task<IActionResult> GetFundingPeriods(HttpRequest request);

        Task<IActionResult> GetFundingPeriodById(HttpRequest request);

        Task<IActionResult> SaveFundingPeriods(HttpRequest request);
    }
}
