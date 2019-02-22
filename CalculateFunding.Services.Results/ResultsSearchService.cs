using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class ResultsSearchService : IResultsSearchService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<ProviderIndex> _searchRepository;
        private readonly Policy _resultsSearchRepositoryPolicy;

        private FacetFilterType[] Facets = {
            new FacetFilterType("authority"),
            new FacetFilterType("providerType"),
            new FacetFilterType("providerSubType")
        };

        private IEnumerable<string> DefaultOrderBy = new[] { "name" };

        public ResultsSearchService(ILogger logger,
            ISearchRepository<ProviderIndex> searchRepository,
            IResultsResilliencePolicies resilliencePolicies)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _logger = logger;
            _searchRepository = searchRepository;
            _resultsSearchRepositoryPolicy = resilliencePolicies.ResultsSearchRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ResultsSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        async public Task<IActionResult> SearchProviders(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SearchModel searchModel = JsonConvert.DeserializeObject<SearchModel>(json);

            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching providers");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            IEnumerable<Task<SearchResults<ProviderIndex>>> searchTasks = BuildSearchTasks(searchModel);

            try
            {
                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());
                ProviderSearchResults providerSearchResults = new ProviderSearchResults();


                foreach (Task<SearchResults<ProviderIndex>> searchTask in searchTasks)
                {
                    ProcessSearchResults(searchTask.Result, searchModel, providerSearchResults);
                }

                return new OkObjectResult(providerSearchResults);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        public Task<IActionResult> GetProviderResults(HttpRequest httpContextRequest)
        {
            throw new System.NotImplementedException();
        }

        IDictionary<string, string> BuildFacetDictionary(SearchModel searchModel)
        {
            if (searchModel.Filters == null)
            {
                searchModel.Filters = new Dictionary<string, string[]>();
            }

            searchModel.Filters = searchModel.Filters.ToList().Where(m => !m.Value.IsNullOrEmpty())
                .ToDictionary(m => m.Key, m => m.Value);

            IDictionary<string, string> facetDictionary = new Dictionary<string, string>();

            foreach (FacetFilterType facet in Facets)
            {
                string filter = "";
                if (searchModel.Filters.ContainsKey(facet.Name) && searchModel.Filters[facet.Name].AnyWithNullCheck())
                {
                    if (facet.IsMulti)
                    {
                        filter = $"({facet.Name}/any(x: {string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"x eq '{x}'"))}))";
                    }
                    else
                    {
                        filter = $"({string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"{facet.Name} eq '{x}'"))})";
                    }
                }
                facetDictionary.Add(facet.Name, filter);
            }

            return facetDictionary;
        }

        IEnumerable<Task<SearchResults<ProviderIndex>>> BuildSearchTasks(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<ProviderIndex>>> searchTasks = new Task<SearchResults<ProviderIndex>>[0];

            if (searchModel.IncludeFacets)
            {
                foreach (KeyValuePair<string, string> filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            IEnumerable<string> s = facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value);

                            return _resultsSearchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                            {
                                Facets = new[]{ filterPair.Key },
                                SearchMode = SearchMode.Any,
                                IncludeTotalResultCount = true,
                                Filter = string.Join(" and ", facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value)),
                                QueryType = QueryType.Full
                            }));
                        })
                    });
                }
            }

            searchTasks = searchTasks.Concat(new[]
            {
                BuildItemsSearchTask(facetDictionary, searchModel)
            });

            return searchTasks;
        }

        Task<SearchResults<ProviderIndex>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            return Task.Run(() =>
            {
                return _resultsSearchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = SearchMode.Any,
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                    OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                }));
            });
        }

        void ProcessSearchResults(SearchResults<ProviderIndex> searchResult, SearchModel searchModel, ProviderSearchResults results)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                results.TotalCount = (int)(searchResult?.TotalCount ?? 0);

                if (!searchModel.CountOnly)
                {
                    results.Results = searchResult?.Results?.Select(m => new ProviderSearchResult
                    {
                        UKPRN = m.Result.UKPRN,
                        URN = m.Result.URN,
                        UPIN = m.Result.UPIN,
                        Rid = m.Result.Rid,
                        ProviderId = m.Result.ProviderId,
                        EstablishmentNumber = m.Result.EstablishmentNumber,
                        Name = m.Result.Name,
                        Authority = m.Result.Authority,
                        ProviderType = m.Result.ProviderType,
                        ProviderSubType = m.Result.ProviderSubType,
                        OpenDate = m.Result.OpenDate,
                        CloseDate = m.Result.CloseDate,
                        ProviderProfileId = m.Result.ProviderId,
                        NavVendorNo = m.Result.NavVendorNo,
                        CrmAccountId = m.Result.CrmAccountId,
                        LegalName = m.Result.LegalName,
                        LACode = m.Result.LACode,
                        ProviderProfileIdType = m.Result.ProviderIdType,
                        Status = m.Result.Status,
                        DfeEstablishmentNumber = m.Result.DfeEstablishmentNumber,
                        PhaseOfEducation = m.Result.PhaseOfEducation,
                        ReasonEstablishmentClosed = m.Result.ReasonEstablishmentClosed,
                        ReasonEstablishmentOpened = m.Result.ReasonEstablishmentOpened,
                        Successor = m.Result.Successor
                    });
                }
            }
        }
    }
}