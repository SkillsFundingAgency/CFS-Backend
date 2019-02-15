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
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Providers.Interfaces;

namespace CalculateFunding.Services.Providers
{
    public class ProviderService : IProviderService
    {
        private const int MaxResultsCount = 1000;
        private const string GetProvidersFromSearchUri = "results/providers-search";

        private readonly ICacheProvider _cacheProvider;
        private readonly IResultsApiClientProxy _resultsApiClient;
        private readonly IMapper _mapper;

        public ProviderService(ICacheProvider cacheProvider, IResultsApiClientProxy resultsApiClient, IMapper mapper)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _cacheProvider = cacheProvider;
            _resultsApiClient = resultsApiClient;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProviderSummary>> FetchCoreProviderData()
        {
            int totalCount = await DetermineTotalCountFromSearch();

            string currentProviderCount = await _cacheProvider.GetAsync<string>(CacheKeys.AllProviderSummaryCount);

            if (string.IsNullOrWhiteSpace(currentProviderCount) || int.Parse(currentProviderCount) != totalCount)
            {
                int pageCount = HowManyPagesForTotalCount(totalCount);

                await _cacheProvider.KeyDeleteAsync<List<ProviderSummary>>(CacheKeys.AllProviderSummaries);

                List<ProviderSummary> providersFromSearch = new List<ProviderSummary>();

                for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
                {
                    IEnumerable<ProviderSummary> summaries = await FetchProviderSummariesFromSearch(pageNumber, MaxResultsCount);
                    providersFromSearch.AddRange(summaries);
                    await _cacheProvider.CreateListAsync(summaries.ToList(), CacheKeys.AllProviderSummaries);
                }

                await _cacheProvider.SetAsync(CacheKeys.AllProviderSummaryCount, totalCount.ToString(), TimeSpan.FromDays(365), true);
                return providersFromSearch;
            }
            else
            {
                return await _cacheProvider.GetAsync<IEnumerable<ProviderSummary>>(CacheKeys.AllProviderSummaries);
            }
        }

        public Task<ProviderSearchResults> SearchProviders(SearchModel searchModel)
        {
            return _resultsApiClient.PostAsync<ProviderSearchResults, SearchModel>(GetProvidersFromSearchUri, searchModel);
        }

        private async Task<IEnumerable<ProviderSummary>> FetchProviderSummariesFromSearch(int pageNumber, int top = 50)
        {
            ProviderSearchResults providers = await SearchProviders(new SearchModel
            {
                PageNumber = pageNumber,
                Top = top,
                IncludeFacets = false
            });

            return _mapper.Map<IEnumerable<ProviderSummary>>(providers.Results);
        }

        private async Task<int> DetermineTotalCountFromSearch()
        {
            ProviderSearchResults providers = await SearchProviders(new SearchModel
            {
                PageNumber = 1,
                Top = 1,
                IncludeFacets = false
            });

            return providers.TotalCount;
        }

        private int HowManyPagesForTotalCount(int totalCount, int maxResultsCount = MaxResultsCount)
        {
            if ((totalCount > 0) && (totalCount < MaxResultsCount))
            {
                return 1;
            }

            int pageCount = totalCount / maxResultsCount;

            if (pageCount % MaxResultsCount != 0)
            {
                pageCount += 1;
            }

            return pageCount;
        }
    }
}
