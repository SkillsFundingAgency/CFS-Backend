using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class ProviderResultsRepository : IProviderResultsRepository
    {
        const int MaxResultsCount = 1000;

        const string getProviderResultsUrl = "results/get-provider-results-by-spec-id?specificationId=";

        const string updateProviderResultsUrl = "results/update-provider-results";

        const string getProvidersFromSearch = "results/providers-search";

        const string getProviderSourceDatasets = "results/get-provider-source-datasets?providerId={0}&specificationId={1}";

        const string getScopedProviderIdsUrl = "results/get-scoped-providerids?specificationId=";

        private readonly IApiClientProxy _apiClient;

        private readonly ICacheProvider _cacheProvider;

        public ProviderResultsRepository(IApiClientProxy apiClient, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _apiClient = apiClient;
            _cacheProvider = cacheProvider;
        }

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{getProviderResultsUrl}{specificationId}";

            return _apiClient.GetAsync<IEnumerable<ProviderResult>>(url);
        }

        public Task<HttpStatusCode> UpdateProviderResults(IEnumerable<ProviderResult> providerResults)
        {
            return _apiClient.PostAsync(updateProviderResultsUrl, providerResults);
        }

        public Task<ProviderSearchResults> SearchProviders(SearchModel searchModel)
        {
            return _apiClient.PostAsync<ProviderSearchResults, SearchModel>(getProvidersFromSearch, searchModel);
        }

        async public Task<IEnumerable<ProviderSummary>> GetProviderSummariesFromCache(int start, int stop)
        {
            await LoadAllProvidersFromSearch();

            return await _cacheProvider.ListRangeAsync<ProviderSummary>(CacheKeys.AllProviderSummaries, start, stop);
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string providerId, string specificationId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentNullException(nameof(providerId));

            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = string.Format(getProviderSourceDatasets, providerId, specificationId);

            return _apiClient.GetAsync<IEnumerable<ProviderSourceDataset>>(url);
        }

        public async Task PopulateProviderSummariesForSpecification(string specificationId)
        {
            IEnumerable<ProviderSummary> allCachedProviders = Enumerable.Empty<ProviderSummary>();

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            string providerCount = await _cacheProvider.GetAsync<string>(CacheKeys.AllProviderSummaryCount);
            int allSummariesCount = 0;

            if (!string.IsNullOrWhiteSpace(providerCount) && !int.TryParse(providerCount, out allSummariesCount))
            {
                throw new Exception("Invalid provider count in cache");
            }

            if(allSummariesCount > 0)
            {
                allCachedProviders = await _cacheProvider.ListRangeAsync<ProviderSummary>(CacheKeys.AllProviderSummaries, 0, allSummariesCount);
            }


            if (allSummariesCount < 1 || allCachedProviders.IsNullOrEmpty())
            {
                allCachedProviders = await LoadAllProvidersFromSearch();
                allSummariesCount = allCachedProviders.Count();
            }

            if (allSummariesCount < 1 || allCachedProviders.IsNullOrEmpty())
            {
                throw new InvalidOperationException($"No provider summaries exist in cache or search");
            }

            IEnumerable<string> providerIds = await GetScopedProviderIds(specificationId);

            IList<ProviderSummary> providerSummaries = new List<ProviderSummary>();

            foreach (string providerId in providerIds)
            {
                ProviderSummary cachedProvider = allCachedProviders.FirstOrDefault(m => m.Id == providerId);

                if (cachedProvider != null)
                {
                    providerSummaries.Add(cachedProvider);
                }
            }

            await _cacheProvider.CreateListAsync<ProviderSummary>(providerSummaries, cacheKey);
        }

        public Task<IEnumerable<string>> GetScopedProviderIds(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{getScopedProviderIdsUrl}{specificationId}";

            return _apiClient.GetAsync<IEnumerable<string>>(url);
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
                Id = x.UKPRN,
                UKPRN = x.UKPRN,
                URN = x.URN,
                Authority = x.Authority,
                UPIN = x.UPIN,
                ProviderSubType = x.ProviderSubType,
                EstablishmentNumber = x.EstablishmentNumber,
                ProviderType = x.ProviderType,
                DateOpened = x.OpenDate
            });
        }

        public async Task<IEnumerable<ProviderSummary>> LoadAllProvidersFromSearch()
        {
            int totalCount = await GetTotalCount();

            string currentProviderCount = await _cacheProvider.GetAsync<string>(CacheKeys.AllProviderSummaryCount);

            IList<int> pageIndexes = new List<int>();

            List<ProviderSummary> providersFromSearch = new List<ProviderSummary>();

            if (string.IsNullOrWhiteSpace(currentProviderCount) || int.Parse(currentProviderCount) != totalCount)
            {
                int pageCount = GetPageCount(totalCount);


                await _cacheProvider.KeyDeleteAsync<List<ProviderSummary>>(CacheKeys.AllProviderSummaries);

                for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
                {
                    IEnumerable<ProviderSummary> summaries = await GetProviderSummaries(pageNumber, MaxResultsCount);
                    providersFromSearch.AddRange(summaries);
                    await _cacheProvider.CreateListAsync(summaries.ToList(), CacheKeys.AllProviderSummaries);
                }

                await _cacheProvider.SetAsync(CacheKeys.AllProviderSummaryCount, totalCount.ToString(), TimeSpan.FromDays(365), true);
            }

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

        int GetPageCount(int totalCount, int maxResultsCount = MaxResultsCount)
        {
            int pageCount = totalCount / maxResultsCount;

            if (pageCount % MaxResultsCount != 0)
                pageCount += 1;

            return pageCount;
        }

    }
}
