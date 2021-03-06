﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationSearchService : ICalculationsSearchService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;

        private readonly FacetFilterType[] Facets = {
            new FacetFilterType("status"),
            new FacetFilterType("specificationName"),
            new FacetFilterType("fundingStreamName")
        };

        private readonly IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };

        public CalculationSearchService(ILogger logger,
            ISearchRepository<CalculationIndex> searchRepository)
        {
            _logger = logger;
            _searchRepository = searchRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationService)
            };
            health.Dependencies.Add(new DependencyHealth 
            { 
                HealthOk = Ok, 
                DependencyName = _searchRepository.GetType().GetFriendlyName(),
                Message = Message 
            });

            return health;
        }

        public async Task<IActionResult> SearchCalculations(string specificationId,
            CalculationType calculationType,
            PublishStatus? status,
            string searchTerm,
            int? page)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            Dictionary<string,string[]> filters = new Dictionary<string, string[]>
            {
                {"specificationId", new []{ specificationId }},
                {"calculationType", new []{ calculationType.ToString() }}
            };

            if (status.HasValue)
            {
                filters.Add("status", new [] { status.Value.ToString() });
            }
            
            return await SearchCalculations(new SearchModel
            {
                SearchMode = Models.Search.SearchMode.All,
                FacetCount = 50,
                SearchTerm = searchTerm,
                Filters    = filters,
                PageNumber = page.GetValueOrDefault(1),
                Top = 50
            });
        }

        public async Task<IActionResult> SearchCalculations(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching calculations");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            if (searchModel.FacetCount < 0 || searchModel.FacetCount > 1000)
            {
                return new BadRequestObjectResult("An invalid facet count was specified");
            }

            IEnumerable<Task<SearchResults<CalculationIndex>>> searchTasks = BuildSearchTasks(searchModel);

            try
            {
                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());
                CalculationSearchResults results = new CalculationSearchResults();

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
                        filter = $"({facet.Name}/any(x: {string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"x eq '{x.Replace("'", "''")}'"))}))";
                    else
                        filter = $"({string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"{facet.Name} eq '{x.Replace("'", "''")}'"))})";
                }
                facetDictionary.Add(facet.Name, filter);
            }

            return facetDictionary;
        }

        IEnumerable<Task<SearchResults<CalculationIndex>>> BuildSearchTasks(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<CalculationIndex>>> searchTasks = new Task<SearchResults<CalculationIndex>>[0];

            if (searchModel.IncludeFacets)
            {
                foreach (var filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                            {
                                Facets = new[]{ $"{filterPair.Key},count:{searchModel.FacetCount}" },
                                SearchMode = (SearchMode)searchModel.SearchMode,
                                SearchFields = new List<string>{ "name" },
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

        Task<SearchResults<CalculationIndex>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;

            searchModel.Filters.Where(_ => !facetDictionary.ContainsKey(_.Key)).ToList().ForEach(_ => facetDictionary.Add(_.Key, $"({string.Join(" or ", _.Value.Select(x => $"{_.Key} eq '{x.Replace("'", "''")}'"))})"));

            return Task.Run(() =>
            {
                return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = (SearchMode)searchModel.SearchMode,
                    SearchFields = new List<string> { "name" },
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                    OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                });
            });
        }

        void ProcessSearchResults(SearchResults<CalculationIndex> searchResult, CalculationSearchResults results)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                results.TotalCount = (int)(searchResult?.TotalCount ?? 0);
                results.Results = searchResult?.Results?.Select(m => new CalculationSearchResult
                {
                    Id = m.Result.Id,
                    Name = m.Result.Name,
                    FundingStreamId = m.Result.FundingStreamId,
                    SpecificationId = m.Result.SpecificationId,
                    ValueType = m.Result.ValueType,
                    CalculationType = m.Result.CalculationType,
                    Namespace = m.Result.Namespace,
                    WasTemplateCalculation = m.Result.WasTemplateCalculation,
                    Description = m.Result.Description,
                    Status = m.Result.Status,
                    LastUpdatedDate = m.Result.LastUpdatedDate?.LocalDateTime,
                    SpecificationName = m.Result.SpecificationName,
                });
            }
        }
    }
}
