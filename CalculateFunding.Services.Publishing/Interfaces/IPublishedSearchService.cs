using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedSearchService
    {
        Task<IActionResult> SearchPublishedProviders(HttpRequest request);
        Task<IActionResult> SearchPublishedProviderLocalAuthorities(string searchText, string fundingStreamId, string fundingPeriodId);
    }
}
