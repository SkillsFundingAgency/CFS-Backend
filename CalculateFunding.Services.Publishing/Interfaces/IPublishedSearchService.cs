using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedSearchService
    {
        Task<IActionResult> SearchPublishedProviders(SearchModel searchModel);
        Task<IActionResult> SearchPublishedProviderIds(PublishedProviderIdSearchModel searchModel);
        Task<IActionResult> SearchPublishedProviderLocalAuthorities(string searchText, string fundingStreamId, string fundingPeriodId);

        Task<IActionResult> GetFundingValue(SearchModel searchModel);
    }
}
