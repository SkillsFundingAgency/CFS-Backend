using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class ProviderRepository : Interfaces.IProviderRepository
    {
        const int MaxResultsCount = 1000;

        const string GetProviderSourceDatasets = "results/get-provider-source-datasets?providerId={0}specificationId={1}";
        const string UpdateProviderSourceDatset = "results/update-provider-source-dataset";
        const string GetProvidersFromSearch = "results/providers-search";

        private readonly IApiClientProxy _apiClient;
        private readonly ICacheProvider _cacheProvider;

        public ProviderRepository(IApiClientProxy apiClient, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _apiClient = apiClient;
            _cacheProvider = cacheProvider;
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

        public Task<HttpStatusCode> UpdateProviderSourceDataset(ProviderSourceDataset providerSourceDataset)
        {
            if(providerSourceDataset == null)
                throw new ArgumentNullException(nameof(providerSourceDataset));

            return _apiClient.PostAsync(UpdateProviderSourceDatset, providerSourceDataset);
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
            int pageCount = totalCount / MaxResultsCount;

            if (pageCount % MaxResultsCount != 0)
                pageCount += 1;

            return pageCount;
        }
    }
}
