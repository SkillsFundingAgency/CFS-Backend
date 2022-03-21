using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Filtering;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Serilog;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetSearchService : IDatasetSearchService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<DatasetIndex> _searchRepository;
        private readonly ISearchRepository<DatasetVersionIndex> _searchVersionRepository;

        private FacetFilterType[] Facets = {
            new FacetFilterType("fundingPeriodNames", true),
            new FacetFilterType("status"),
            new FacetFilterType("definitionName"),
            new FacetFilterType("specificationNames", true),
            new FacetFilterType("fundingStreamId"),
            new FacetFilterType("fundingStreamName")
        };

        private IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };

        public DatasetSearchService(ILogger logger,
            ISearchRepository<DatasetIndex> searchRepository,
            ISearchRepository<DatasetVersionIndex> searchVersionRepository)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(searchVersionRepository, nameof(searchVersionRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _logger = logger;
            _searchRepository = searchRepository;
            _searchVersionRepository = searchVersionRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var searchRepoHealth = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(DatasetService)
            };

            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        async public Task<IActionResult> SearchDatasets(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching datasets");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            IEnumerable<Task<SearchResults<DatasetIndex>>> searchTasks = BuildSearchTasks(searchModel);

            try
            {
                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

                DatasetSearchResults results = new DatasetSearchResults();
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

        async public Task<IActionResult> SearchDatasetVersion(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching datasets");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            IDictionary<string, string[]> searchModelDictionary = searchModel.Filters;

            List<Filter> filters = searchModelDictionary.Select(keyValueFilterPair => new Filter(keyValueFilterPair.Key, keyValueFilterPair.Value, false, "eq")).ToList();

            FilterHelper filterHelper = new FilterHelper(filters);

            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            SearchParameters searchParameters = new SearchParameters()
            {
                Filter = filterHelper.BuildAndFilterQuery(),
                IncludeTotalResultCount = true,
                OrderBy = new[] { "version desc" },
                Skip = skip,
                Top = searchModel.Top
            };

            SearchResults<DatasetVersionIndex> searchResults = await _searchVersionRepository.Search(searchModel.SearchTerm, searchParameters);

            DatasetVersionSearchResults datasetVersionSearchResults = new DatasetVersionSearchResults()
            {
                TotalCount = (int)(searchResults?.TotalCount ?? 0),
                Results = searchResults.Results.Select(ConvertToDatasetVersionSearchResult)
            };

            return new OkObjectResult(datasetVersionSearchResults);
        }

        private DatasetVersionSearchResult ConvertToDatasetVersionSearchResult(Repositories.Common.Search.SearchResult<DatasetVersionIndex> sr)
        {
            return new DatasetVersionSearchResult
            {
                Name = sr.Result.Name,
                Id = sr.Result.Id,
                Version = sr.Result.Version,
                BlobName = sr.Result.BlobName,
                ChangeNote = sr.Result.ChangeNote,
                ChangeType = sr.Result.ChangeType,
                Description = sr.Result.Description,
                DatasetId = sr.Result.DatasetId,
                LastUpdatedByName = sr.Result.LastUpdatedByName,
                DefinitionName = sr.Result.DefinitionName,
                LastUpdatedDate = sr.Result.LastUpdatedDate,
                FundingStreamId = sr.Result.FundingStreamId,
                FundingStreamName = sr.Result.FundingStreamName
            };
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

        IEnumerable<Task<SearchResults<DatasetIndex>>> BuildSearchTasks(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<DatasetIndex>>> searchTasks = new Task<SearchResults<DatasetIndex>>[0];

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
                                Facets = new[]{ $"{filterPair.Key},count:{searchModel.FacetCount}" },
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

        Task<SearchResults<DatasetIndex>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
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

        void ProcessSearchResults(SearchResults<DatasetIndex> searchResult, DatasetSearchResults results)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                results.TotalCount = (int)(searchResult?.TotalCount ?? 0);
                results.Results = searchResult?.Results?.Select(m => new DatasetSearchResult
                {
                    Id = m.Result.Id,
                    Name = m.Result.Name,
                    Status = m.Result.Status,
                    DefinitionName = m.Result.DefinitionName,
                    LastUpdatedDate = m.Result.LastUpdatedDate.LocalDateTime,
                    PeriodNames = m.Result.FundingPeriodNames,
                    SpecificationNames = m.Result.SpecificationNames,
                    Description = m.Result.Description,
                    Version = m.Result.Version,
                    ChangeNote = m.Result.ChangeNote,
                    ChangeType = m.Result.ChangeType,
                    LastUpdatedByName = m.Result.LastUpdatedByName,
                    LastUpdatedById = m.Result.LastUpdatedById,
                    FundingStreamId = m.Result.FundingStreamId,
                    FundingStreamName = m.Result.FundingStreamName
                });
            }
        }
    }
}
