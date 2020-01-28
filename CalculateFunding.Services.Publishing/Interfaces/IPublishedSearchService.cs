using System.Threading.Tasks;
using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedSearchService
    {
        Task<IActionResult> SearchPublishedProviders(SearchModel searchModel);
        Task<IActionResult> SearchPublishedProviderLocalAuthorities(string searchText, string fundingStreamId, string fundingPeriodId);
    }
}
