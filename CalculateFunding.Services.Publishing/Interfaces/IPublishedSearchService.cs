using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedSearchService : IHealthChecker
    {
        Task<IActionResult> SearchPublishedProviders(SearchModel searchModel);
        Task<IActionResult> SearchPublishedProviderLocalAuthorities(string searchText, string fundingStreamId, string fundingPeriodId);
    }
}
