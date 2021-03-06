﻿using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionSearchService
    {
        Task<IActionResult> SearchProviders(string providerVersionId, SearchModel searchModel = null);
        Task<IActionResult> SearchProviders(int year, int month, int day, SearchModel searchModel);
        Task<IActionResult> GetProviderById(string providerVersionId, string providerId);
        Task<IActionResult> GetProviderById(int year, int month, int day, string providerId);
        Task<IActionResult> SearchProviderVersions(SearchModel searchModel);
        Task<IActionResult> GetFacetValues(string facetName);
        Task<IActionResult> GetLocalAuthoritiesByProviderVersionId(string providerVersionId);
        Task<IActionResult> GetLocalAuthoritiesByFundingStreamId(string fundingStreamId);
    }
}
