using CalculateFunding.Models;
using CalculateFunding.Models.Results;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class CalculationProviderResultsSearchService : ICalculationProviderResultsSearchService
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<CalculationProviderResultsIndex> _searchRepository;
        private readonly Policy _searchRepositoryPolicy;

        private FacetFilterType[] Facets = {
            new FacetFilterType("calculationId"),
            new FacetFilterType("calculationSpecificationId"),
            new FacetFilterType("specificationName"),
            new FacetFilterType("specificationId"),
            new FacetFilterType("calculationSpecificationName"),
            new FacetFilterType("providerName"),
            new FacetFilterType("providerType"),
            new FacetFilterType("providerSubType"),
            new FacetFilterType("providerId"),
            new FacetFilterType("localAuthority")
        };

        private IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };
        
        public CalculationProviderResultsSearchService(ILogger logger,
            ISearchRepository<CalculationProviderResultsIndex> searchRepository,
            IResultsResilliencePolicies resiliencePolicies)
        {
            _logger = logger;
            _searchRepository = searchRepository;
            _searchRepositoryPolicy = resiliencePolicies.CalculationProviderResultsSearchRepository;
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
            IEnumerable<Task<SearchResults<CalculationProviderResultsIndex>>> searchTasks = BuildSearchTasks(searchModel);

            await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

            CalculationProviderResultSearchResults results = new CalculationProviderResultSearchResults();

            foreach (var searchTask in searchTasks)
                ProcessSearchResults(searchTask.Result, results);

            return results;
        }

        IDictionary<string, string> BuildFacetDictionary(SearchModel searchModel)
        {
            if (searchModel.Filters == null)
                searchModel.Filters = new Dictionary<string, string[]>();

            searchModel.Filters = searchModel.Filters.ToList().Where(m => !m.Value.IsNullOrEmpty())
                .ToDictionary(m => m.Key, m => m.Value);

            IDictionary<string, string> facetDictionary = new Dictionary<string, string>();

            foreach (var facet in Facets)
            {
                string filter = "";
                if (searchModel.Filters.ContainsKey(facet.Name) && searchModel.Filters[facet.Name].AnyWithNullCheck())
                {
                    if (facet.IsMulti)
                        filter = $"({facet.Name}/any(x: {string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"x eq '{x}'"))}))";
                    else
                        filter = $"({string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"{facet.Name} eq '{x}'"))})";

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

        IEnumerable<Task<SearchResults<CalculationProviderResultsIndex>>> BuildSearchTasks(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<CalculationProviderResultsIndex>>> searchTasks = new Task<SearchResults<CalculationProviderResultsIndex>>[0];

            if (searchModel.IncludeFacets)
            {
                foreach (var filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            var s = facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value);

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

        Task<SearchResults<CalculationProviderResultsIndex>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
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

        private void ProcessSearchResults(SearchResults<CalculationProviderResultsIndex> searchResult, CalculationProviderResultSearchResults results)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                results.TotalCount = (int)(searchResult?.TotalCount ?? 0);
                results.Results = searchResult?.Results?.Select(m => new CalculationProviderResultSearchResult
                {
                    Id = m.Result.Id,
                    ProviderId = m.Result.ProviderId,
                    ProviderName = m.Result.ProviderName,
                    SpecificationName = m.Result.SpecificationName,
                    SpecificationId = m.Result.SpecificationId,
                    CalculationSpecificationId = m.Result.CalculationSpecificationId,
                    CalculationSpecificationName = m.Result.CalculationSpecificationName,
                    CalculationId = m.Result.CalculationId,
                    CalculationName = m.Result.CalculationName,
                    CalculationType = m.Result.CalculationType,
                    CaclulationResult = m.Result.CaclulationResult,
                    LastUpdatedDate = m.Result.LastUpdatedDate,
                    LocalAuthority = m.Result.LocalAuthority,
                    ProviderType = m.Result.ProviderType,
                    ProviderSubType = m.Result.ProviderSubType,
                    UKPRN = m.Result.UKPRN,
                    UPIN = m.Result.UPIN,
                    URN = m.Result.URN,
                    OpenDate = m.Result.OpenDate,
                    EstablishmentNumber = m.Result.EstablishmentNumber
                });
            }
        }
    }
}
