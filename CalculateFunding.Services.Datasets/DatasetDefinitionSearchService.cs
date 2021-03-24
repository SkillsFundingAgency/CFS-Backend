using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Serilog;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetDefinitionSearchService : IDatasetDefinitionSearchService
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<DatasetDefinitionIndex> _searchRepository;

        private FacetFilterType[] Facets = {
            new FacetFilterType("providerIdentifier", true),
            new FacetFilterType("fundingStreamId"),
            new FacetFilterType("fundingStreamName"),
        };

        private IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };

        public DatasetDefinitionSearchService(ILogger logger,
            ISearchRepository<DatasetDefinitionIndex> searchRepository)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _logger = logger;
            _searchRepository = searchRepository;
        }

        async public Task<IActionResult> SearchDatasetDefinitions(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching datasets");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            IEnumerable<Task<SearchResults<DatasetDefinitionIndex>>> searchTasks = BuildSearchTasks(searchModel);

            try
            {
                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());
                DatasetDefinitionSearchResults results = new DatasetDefinitionSearchResults();
                foreach (var searchTask in searchTasks)
                {
                    ProcessSearchResults(searchTask.Result, results);
                }

                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
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
                facetDictionary.Add(facet.Name, filter);
            }

            return facetDictionary;
        }

        IEnumerable<Task<SearchResults<DatasetDefinitionIndex>>> BuildSearchTasks(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<DatasetDefinitionIndex>>> searchTasks = new Task<SearchResults<DatasetDefinitionIndex>>[0];

            if (searchModel.IncludeFacets)
            {
                foreach (var filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            var s = facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value);

                            return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                            {
                                Facets = new[]{ filterPair.Key },
                                SearchMode = (SearchMode)searchModel.SearchMode,
                                IncludeTotalResultCount = true,
                                Filter = string.Join(" and ", facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value)),
                                QueryType = QueryType.Full
                            });
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

        Task<SearchResults<DatasetDefinitionIndex>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            return Task.Run(() =>
            {
                return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = (SearchMode)searchModel.SearchMode,
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                    OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                });
            });
        }

        void ProcessSearchResults(SearchResults<DatasetDefinitionIndex> searchResult, DatasetDefinitionSearchResults results)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                results.TotalCount = (int)(searchResult?.TotalCount ?? 0);
                results.Results = searchResult?.Results?.Select(m => new DatasetDefinitionSearchResult
                {
                    Id = m.Result.Id,
                    Name = m.Result.Name,
                    Description = m.Result.Description,
                    LastUpdatedDate = m.Result.LastUpdatedDate,
                    ProviderIdentifier = m.Result.ProviderIdentifier,
                    FundingStreamId = m.Result.FundingStreamId,
                    FundingStreamName = m.Result.FundingStreamName,
                    ConverterEnabled = m.Result.ConverterEnabled
                });
            }
        }
    }
}
