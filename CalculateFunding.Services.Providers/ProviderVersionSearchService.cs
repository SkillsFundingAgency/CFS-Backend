using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionSearchService : IProviderVersionSearchService
    {
        private readonly ICacheProvider _cacheProvider;

        public ProviderVersionSearchService(ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _cacheProvider = cacheProvider;
        }

        public async Task<IActionResult> GetProviderById(string providerVersionId, string providerId)
        {
            throw new NotImplementedException();
        }

        public async Task<IActionResult> GetProviderById(int year, int month, int day, string providerId)
        {
            throw new NotImplementedException();
        }

        public async Task<IActionResult> SearchProviders(string providerVersionId, SearchModel searchModel)
        {
            throw new NotImplementedException();
        }

        public async Task<IActionResult> SearchProviders(int year, int month, int day, SearchModel searchModel)
        {
            throw new NotImplementedException();
        }

        public async Task<IActionResult> SearchProviderVersions(SearchModel searchModel)
        {
            throw new NotImplementedException();
        }
    }
}
