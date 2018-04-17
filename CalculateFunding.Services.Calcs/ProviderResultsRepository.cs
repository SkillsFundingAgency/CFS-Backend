using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
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

        const string cachedProvidersKey = "cached-providers-key";

        const string getProviderResultsUrl = "results/get-provider-results-by-spec-id?specificationId=";

        const string updateProviderResultsUrl = "results/update-provider-results";

        const string getProvidersFromSearch = "results/providers-search";

        const string getProviderSourceDatasets = "results/get-provider-source-datasets?providerId={0}&specificationId={1}";

        private readonly IApiClientProxy _apiClient;

        private readonly ICacheProvider _cacheProvider;

        private static IEnumerable<ProviderSummary> _providerSummaries;


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

        //async public Task<IEnumerable<ProviderSummary>> GetAllProviderSummaries()
        //{
        //    if (_providerSummaries.IsNullOrEmpty())
        //    {
        //        List<ProviderSummary> providersFromSearch = await _cacheProvider.GetAsync<List<ProviderSummary>>(cachedProvidersKey);

        //        if (providersFromSearch.IsNullOrEmpty())
        //        {
        //            providersFromSearch = (await LoadAllProvidersFromSearch()).ToList();
        //        }

        //        _providerSummaries = providersFromSearch;
        //    }

        //    return _providerSummaries;
        //}

  

        async public Task<IEnumerable<ProviderSummary>> GetProviderSummariesFromCache(int start, int stop)
        {
            string cacheKey = "all-cached-providers";

            await LoadAllProvidersFromSearch();

            return await _cacheProvider.ListRangeAsync<ProviderSummary>(cacheKey, start, stop);
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

        async public Task<int> LoadAllProvidersFromSearch()
        {
            string cacheKey = "all-cached-providers";

            int totalCount = await GetTotalCount();

            string currentProviderCount = await _cacheProvider.GetAsync<string>("provider-summary-count");

            IList<int> pageIndexes = new List<int>();

            if (string.IsNullOrWhiteSpace(currentProviderCount) || int.Parse(currentProviderCount) != totalCount)
            {
                int pageCount = GetPageCount(totalCount);

                List<ProviderSummary> providersFromSearch = new List<ProviderSummary>();

                await _cacheProvider.KeyDeleteAsync<List<ProviderSummary>>(cacheKey);

                for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
                {
                    IEnumerable<ProviderSummary> summaries = (await GetProviderSummaries(pageNumber, MaxResultsCount)).ToList();
                    await _cacheProvider.CreateListAsync(summaries.ToList(), cacheKey);
                }

                await _cacheProvider.SetAsync("provider-summary-count", totalCount.ToString(), TimeSpan.FromDays(7), true);
            }

            return totalCount;
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
