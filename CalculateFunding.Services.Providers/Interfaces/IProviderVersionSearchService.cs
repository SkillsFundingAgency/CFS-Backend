using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionSearchService
    {
        Task<IActionResult> SearchProviders(string providerVersionId, SearchModel searchModel = null);
        Task<IActionResult> SearchProviders(int year, int month, int day, SearchModel searchModel);
        Task<IActionResult> SearchMasterProviders(SearchModel searchModel);
        Task<IActionResult> GetProviderByIdFromMaster(string providerId);
        Task<IActionResult> GetProviderById(string providerVersionId, string providerId);
        Task<IActionResult> GetProviderById(int year, int month, int day, string providerId);
        Task<IActionResult> SearchProviderVersions(SearchModel searchModel);
    }
}
