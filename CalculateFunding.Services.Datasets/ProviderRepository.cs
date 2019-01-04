using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;

namespace CalculateFunding.Services.Datasets
{
    public class ProviderRepository : Interfaces.IProviderRepository, IHealthChecker
    {
        const int MaxResultsCount = 1000;

        const string GetProviderSourceDatasets = "results/get-provider-source-datasets?providerId={0}specificationId={1}";
        const string GetProvidersFromSearch = "results/providers-search";

        private readonly IResultsApiClientProxy _apiClient;
        private readonly ICacheProvider _cacheProvider;

        public ProviderRepository(IResultsApiClientProxy apiClient, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _apiClient = apiClient;
            _cacheProvider = cacheProvider;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

            return health;
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string providerId, string specificationId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentNullException(nameof(providerId));

            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = string.Format(GetProviderSourceDatasets, providerId, specificationId);

            return _apiClient.GetAsync<IEnumerable<ProviderSourceDataset>>(url);
        }

        public Task<ProviderSearchResults> SearchProviders(SearchModel searchModel)
        {
            return _apiClient.PostAsync<ProviderSearchResults, SearchModel>(GetProvidersFromSearch, searchModel);
        }

        async public Task<IEnumerable<ProviderSummary>> GetAllProviderSummaries()
        {
            List<ProviderSummary> providersFromSearch = await _cacheProvider.GetAsync<List<ProviderSummary>>(CacheKeys.AllProviderSummaries);

            if (providersFromSearch.IsNullOrEmpty())
            {
                providersFromSearch = (await LoadAllProvidersFromSearch()).ToList();

                await _cacheProvider.SetAsync<List<ProviderSummary>>(CacheKeys.AllProviderSummaries, providersFromSearch, TimeSpan.FromDays(7), true);
            }

            return providersFromSearch;
        }

        async Task<IEnumerable<ProviderSummary>> GetProviderSummaries(int pageNumber, int top = 50)
        {
            ProviderSearchResults providers = await SearchProviders(new SearchModel
            {
                PageNumber = pageNumber,
                Top = top,
                IncludeFacets = false
            });

            IEnumerable<ProviderSearchResult> searchResults = providers.Results;

            return searchResults.Select(x => new ProviderSummary
            {
                Name = x.Name,
                Id = x.ProviderProfileId,
                ProviderProfileIdType = x.ProviderProfileIdType,
                UKPRN = x.UKPRN,
                URN = x.URN,
                Authority = x.Authority,
                UPIN = x.UPIN,
                ProviderSubType = x.ProviderSubType,
                EstablishmentNumber = x.EstablishmentNumber,
                ProviderType = x.ProviderType,
                DateOpened = x.OpenDate,
                DateClosed = x.CloseDate,
                LACode = x.LACode,
                CrmAccountId = x.CrmAccountId,
                LegalName = x.LegalName,
                NavVendorNo = x.NavVendorNo,
                DfeEstablishmentNumber = x.DfeEstablishmentNumber,
                Status = x.Status
            });
        }

        async Task<IEnumerable<ProviderSummary>> LoadAllProvidersFromSearch()
        {
            int totalCount = await GetTotalCount();

            int pageCount = GetPageCount(totalCount);

            List<ProviderSummary> providersFromSearch = new List<ProviderSummary>();

            for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
            {
                IEnumerable<ProviderSummary> summaries = (await GetProviderSummaries(pageNumber, MaxResultsCount)).ToList();

                providersFromSearch.AddRange(summaries);
            }

            await _cacheProvider.SetAsync(CacheKeys.AllProviderSummaries, providersFromSearch, TimeSpan.FromDays(7), true);

            return providersFromSearch;
        }

        async Task<int> GetTotalCount()
        {
            ProviderSearchResults providers = await SearchProviders(new SearchModel
            {
                PageNumber = 1,
                Top = 1,
                IncludeFacets = false
            });

            return providers.TotalCount;
        }

        int GetPageCount(int totalCount)
        {
            if ((totalCount > 0) && (totalCount < MaxResultsCount))
                return 1;

            int pageCount = totalCount / MaxResultsCount;

            if (pageCount % MaxResultsCount != 0)
                pageCount += 1;

            return pageCount;
        }
    }
}
