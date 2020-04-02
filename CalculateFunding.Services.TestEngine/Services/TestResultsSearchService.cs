﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using Polly;
using Serilog;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestResultsSearchService : ITestResultsSearchService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<TestScenarioResultIndex> _searchRepository;
        private readonly AsyncPolicy _searchRepositoryPolicy;

        private readonly FacetFilterType[] Facets = {
            new FacetFilterType("testResult"),
            new FacetFilterType("testScenarioId"),
            new FacetFilterType("specificationName"),
            new FacetFilterType("specificationId"),
            new FacetFilterType("testScenarioName"),
            new FacetFilterType("providerName"),
            new FacetFilterType("providerType"),
            new FacetFilterType("providerSubType"),
            new FacetFilterType("providerId"),
            new FacetFilterType("localAuthority")
        };

        private IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };

        public TestResultsSearchService(ILogger logger,
            ISearchRepository<TestScenarioResultIndex> searchRepository,
            ITestRunnerResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.TestResultsSearchRepository, nameof(resiliencePolicies.TestResultsSearchRepository));
            
            _logger = logger;
            _searchRepository = searchRepository;
            _searchRepositoryPolicy = resiliencePolicies.TestResultsSearchRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(TestResultsSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        async public Task<IActionResult> SearchTestScenarioResults(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching calculations");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                TestScenarioSearchResults results = await SearchTestScenarioResultsInternal(searchModel);

                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        public async Task<TestScenarioSearchResults> SearchTestScenarioResultsInternal(SearchModel searchModel)
        {
            IEnumerable<Task<SearchResults<TestScenarioResultIndex>>> searchTasks = BuildSearchTasks(searchModel);

            await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

            TestScenarioSearchResults results = new TestScenarioSearchResults();

            foreach (Task<SearchResults<TestScenarioResultIndex>> searchTask in searchTasks)
            {
                ProcessSearchResults(searchTask.Result, results);
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

        IEnumerable<Task<SearchResults<TestScenarioResultIndex>>> BuildSearchTasks(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<TestScenarioResultIndex>>> searchTasks = new Task<SearchResults<TestScenarioResultIndex>>[0];

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
                                SearchFields = searchModel.SearchFields?.ToList(),
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

        Task<SearchResults<TestScenarioResultIndex>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            return Task.Run(() =>
            {
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = SearchMode.Any,
                    SearchFields = searchModel.SearchFields?.ToList(),
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                    OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                }));
            });
        }

        private void ProcessSearchResults(SearchResults<TestScenarioResultIndex> searchResult, TestScenarioSearchResults results)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                results.TotalCount = (int)(searchResult?.TotalCount ?? 0);
                results.Results = searchResult?.Results?.Select(m => new TestScenarioSearchResult
                {
                    Id = m.Result.Id,
                    ProviderId = m.Result.ProviderId,
                    ProviderName = m.Result.ProviderName,
                    SpecificationName = m.Result.SpecificationName,
                    SpecificationId = m.Result.SpecificationId,
                    TestResult = m.Result.TestResult,
                    TestScenarioId = m.Result.TestScenarioId,
                    TestScenarioName = m.Result.TestScenarioName,
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
