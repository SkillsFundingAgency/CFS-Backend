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

        async public Task<IEnumerable<ProviderSummary>> GetAllProviderSummaries()
        {
            if (_providerSummaries.IsNullOrEmpty())
            {
                List<ProviderSummary> providersFromSearch = await _cacheProvider.GetAsync<List<ProviderSummary>>(cachedProvidersKey);

                if (providersFromSearch.IsNullOrEmpty())
                {
                    providersFromSearch = (await LoadAllProvidersFromSearch()).ToList();
                }

                _providerSummaries = providersFromSearch;
            }

            return _providerSummaries;
        }

        async public Task<int> PartitionProviderSummaries(int partitionSize)
        {
            IEnumerable<ProviderSummary> summaries = await GetAllProviderSummaries();

            int totalCount = await GetTotalCount();

            int pageCount = GetPageCount(totalCount, partitionSize);

            for (int pageNumber = 0; pageNumber < pageCount; pageNumber++)
            {
                string cacheKey = $"provider-summaries-{pageNumber + 1}";

                List<ProviderSummary> providerSummaries = summaries.Skip(pageNumber * partitionSize).Take(partitionSize).ToList();

                bool keyExists = await _cacheProvider.KeyExists<List<ProviderSummary>>(cacheKey);

                if(!keyExists)
                    await _cacheProvider.SetAsync(cacheKey, providerSummaries, TimeSpan.FromDays(7), true);
            }

            return pageCount;
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
                ProviderType = x.ProviderType
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

            await _cacheProvider.SetAsync(cachedProvidersKey, providersFromSearch, TimeSpan.FromDays(7), true);

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
