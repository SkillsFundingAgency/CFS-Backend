using System;
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
    public class ProviderCalculationResultsSearchService : IProviderCalculationResultsSearchService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<ProviderCalculationResultsIndex> _searchRepository;
        private readonly Policy _searchRepositoryPolicy;

        private FacetFilterType[] Facets = {
            new FacetFilterType("calculationId", true),
            new FacetFilterType("calculationName", true),
            new FacetFilterType("specificationName"),
            new FacetFilterType("specificationId"),
            new FacetFilterType("providerName"),
            new FacetFilterType("providerType"),
            new FacetFilterType("providerSubType"),
            new FacetFilterType("providerId"),
            new FacetFilterType("localAuthority")
        };

        private IEnumerable<string> DefaultOrderBy = new[] { "providerName" };

        public ProviderCalculationResultsSearchService(ILogger logger,
            ISearchRepository<ProviderCalculationResultsIndex> searchRepository,
            IResultsResilliencePolicies resiliencePolicies)
        {
            _logger = logger;
            _searchRepository = searchRepository;
            _searchRepositoryPolicy = resiliencePolicies.ProviderCalculationResultsSearchRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderCalculationResultsSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        async public Task<IActionResult> SearchCalculationProviderResults(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SearchModel searchModel = JsonConvert.DeserializeObject<SearchModel>(json);

            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching calculation provider results");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                CalculationProviderResultSearchResults results = await SearchCalculationProviderResults(searchModel);
                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        public async Task<CalculationProviderResultSearchResults> SearchCalculationProviderResults(SearchModel searchModel)
        {
            string calculationId = (searchModel.Filters != null && 
                                        searchModel.Filters.ContainsKey("calculationId") && 
                                        searchModel.Filters["calculationId"].FirstOrDefault() != null) ? searchModel.Filters["calculationId"].FirstOrDefault() : "";

            IEnumerable <Task<SearchResults<ProviderCalculationResultsIndex>>> searchTasks = BuildSearchTasks(searchModel);

            await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

            CalculationProviderResultSearchResults results = new CalculationProviderResultSearchResults();

            foreach (Task<SearchResults<ProviderCalculationResultsIndex>> searchTask in searchTasks)
            {
                ProcessSearchResults(searchTask.Result, results, calculationId);
            }

            return results;
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

                if (searchModel.OverrideFacetFields.Any() && (searchModel.OverrideFacetFields.Contains(facet.Name) || searchModel.Filters.ContainsKey(facet.Name)))
                {
                    facetDictionary.Add(facet.Name, filter);
                }
                else if (!searchModel.OverrideFacetFields.Any())
                {
                    facetDictionary.Add(facet.Name, filter);
                }
            }

            return facetDictionary;
        }

        IEnumerable<Task<SearchResults<ProviderCalculationResultsIndex>>> BuildSearchTasks(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<ProviderCalculationResultsIndex>>> searchTasks = new Task<SearchResults<ProviderCalculationResultsIndex>>[0];

            if (searchModel.IncludeFacets)
            {
                foreach (KeyValuePair<string, string> filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            IEnumerable<string> s = facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value);

                            return _searchRepositoryPolicy.ExecuteAsync(()=> _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                            {
                                Facets = new[]{ filterPair.Key },
                                SearchMode = SearchMode.Any,
                                SearchFields = new List<string>{ "providerName" },
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

        Task<SearchResults<ProviderCalculationResultsIndex>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            return Task.Run(() =>
            {
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = SearchMode.Any,
                    SearchFields = new List<string> { "providerName" },
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                    OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                }));
            });
        }

        private void ProcessSearchResults(SearchResults<ProviderCalculationResultsIndex> searchResult, CalculationProviderResultSearchResults results, string calculationId)
        {
            
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                IList<CalculationProviderResultSearchResult> calculationResults = new List<CalculationProviderResultSearchResult>();

                results.TotalCount = (int)(searchResult?.TotalCount ?? 0);

                if (!searchResult.Results.IsNullOrEmpty())
                {
                    foreach (CalculateFunding.Repositories.Common.Search.SearchResult<ProviderCalculationResultsIndex> result in searchResult.Results)
                    {
                        int calculationIdIndex = Array.IndexOf(result.Result.CalculationId, calculationId);

                        if(calculationIdIndex < 0)
                        {
                            throw new Exception();
                        }

                        CalculationProviderResultSearchResult calculationResult = new CalculationProviderResultSearchResult
                        {
                            Id = result.Result.Id,
                            ProviderId = result.Result.ProviderId,
                            ProviderName = result.Result.ProviderName,
                            SpecificationName = result.Result.SpecificationName,
                            SpecificationId = result.Result.SpecificationId,
                            LastUpdatedDate = result.Result.LastUpdatedDate.LocalDateTime,
                            LocalAuthority = result.Result.LocalAuthority,
                            ProviderType = result.Result.ProviderType,
                            ProviderSubType = result.Result.ProviderSubType,
                            UKPRN = result.Result.UKPRN,
                            UPIN = result.Result.UPIN,
                            URN = result.Result.URN,
                            OpenDate = result.Result.OpenDate,
                            EstablishmentNumber = result.Result.EstablishmentNumber,
                            CalculationId = calculationId,
                            CalculationName = result.Result.CalculationName[calculationIdIndex],
                            CalculationResult = result.Result.CalculationResult[calculationIdIndex].GetValueOrNull<double>()
                        };

                        calculationResults.Add(calculationResult);
                    }
                }

                results.Results = calculationResults;
            }
        }
    }
}
