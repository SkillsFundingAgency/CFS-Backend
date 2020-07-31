using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class ProviderCalculationResultsSearchService : IProviderCalculationResultsSearchService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<ProviderCalculationResultsIndex> _searchRepository;
        private readonly AsyncPolicy _searchRepositoryPolicy;
        private readonly IFeatureToggle _featureToggle;

        private readonly FacetFilterType[] Facets = {
            new FacetFilterType("calculationId", true),
            new FacetFilterType("calculationName", true),
            new FacetFilterType("specificationName"),
            new FacetFilterType("specificationId"),
            new FacetFilterType("providerName"),
            new FacetFilterType("providerType"),
            new FacetFilterType("providerSubType"),
            new FacetFilterType("providerId"),
            new FacetFilterType("localAuthority"),
            new FacetFilterType("fundingLineId", true),
            new FacetFilterType("fundingLineName", true),
        };

        private IEnumerable<string> DefaultOrderBy = new[] { "providerName" };

        public ProviderCalculationResultsSearchService(ILogger logger,
            ISearchRepository<ProviderCalculationResultsIndex> searchRepository,
            IResultsResiliencePolicies resiliencePolicies,
            IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.ProviderCalculationResultsSearchRepository, nameof(resiliencePolicies.ProviderCalculationResultsSearchRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _logger = logger;
            _searchRepository = searchRepository;
            _searchRepositoryPolicy = resiliencePolicies.ProviderCalculationResultsSearchRepository;
            _featureToggle = featureToggle;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderCalculationResultsSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        async public Task<IActionResult> SearchCalculationProviderResults(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching calculation provider results");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                CalculationProviderResultSearchResults results = await SearchCalculationProviderResultsInternal(searchModel);
                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        private async Task<CalculationProviderResultSearchResults> SearchCalculationProviderResultsInternal(SearchModel searchModel)
        {
            string calculationId = GetSearchModelFilterValue(searchModel, "calculationId");
            string fundingLineId = GetSearchModelFilterValue(searchModel, "fundingLineId");

            IEnumerable<Task<SearchResults<ProviderCalculationResultsIndex>>> searchTasks = BuildSearchTasks(searchModel, calculationId, fundingLineId);

            await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

            CalculationProviderResultSearchResults results = new CalculationProviderResultSearchResults();

            foreach (Task<SearchResults<ProviderCalculationResultsIndex>> searchTask in searchTasks)
            {
                ProcessSearchResults(searchTask.Result, results, calculationId, fundingLineId);
            }

            return results;
        }

        private string GetSearchModelFilterValue(SearchModel searchModel, string filterName)
        {
            return (searchModel.Filters != null &&
                                        searchModel.Filters.ContainsKey(filterName) &&
                                        searchModel.Filters[filterName].FirstOrDefault() != null)
                ? searchModel.Filters[filterName].FirstOrDefault()
                : string.Empty;
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

        IEnumerable<Task<SearchResults<ProviderCalculationResultsIndex>>> BuildSearchTasks(SearchModel searchModel, string calculationId, string fundingLineId)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<ProviderCalculationResultsIndex>>> searchTasks = Array.Empty<Task<SearchResults<ProviderCalculationResultsIndex>>>();

            if (searchModel.IncludeFacets)
            {
                foreach (KeyValuePair<string, string> filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            return _searchRepositoryPolicy.ExecuteAsync(()=> _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                            {
                                Facets = new[]{ filterPair.Key },
                                SearchMode = (SearchMode)searchModel.SearchMode,
                                SearchFields = new List<string>{ "providerName" },
                                IncludeTotalResultCount = true,
                                Filter = string.Join(" and ", facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value)),
                                QueryType = QueryType.Full
                            }));
                        })
                    });
                }
            }

            if (_featureToggle.IsExceptionMessagesEnabled())
            {
                if (facetDictionary.ContainsKey("calculationId"))
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        BuildCalculationErrorsSearchTask(facetDictionary["calculationId"], calculationId)
                    });
                }

                if (facetDictionary.ContainsKey("fundingLineId"))
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        BuildFundingLineErrorsSearchTask(facetDictionary["fundingLineId"], fundingLineId)
                    });
                }
            }

            if (facetDictionary.ContainsKey("calculationId"))
            {
                searchTasks = searchTasks.Concat(new[]
                {
                    BuildCalculationItemsSearchTask(facetDictionary, searchModel, calculationId)
                });
            }

            if (facetDictionary.ContainsKey("fundingLineId"))
            {
                searchTasks = searchTasks.Concat(new[]
                {
                    BuildFundingLineItemsSearchTask(facetDictionary, searchModel, fundingLineId)
                });
            }

            return searchTasks;
        }

        Task<SearchResults<ProviderCalculationResultsIndex>> BuildCalculationErrorsSearchTask(string calculationIdFilter, string calculationId)
        {
            return Task.Run(() =>
            {
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(null, new SearchParameters
                {
                    Facets = new[] { "calculationId", "calculationException" },
                    Top = 0,
                    SearchMode = SearchMode.All,
                    IncludeTotalResultCount = true,
                    Filter = calculationIdFilter + $" and calculationException/any(x: x eq '{calculationId}')",
                    QueryType = QueryType.Full
                }));
            });
        }

        Task<SearchResults<ProviderCalculationResultsIndex>> BuildFundingLineErrorsSearchTask(string fundingLineIdFilter, string fundingLineId)
        {
            return Task.Run(() =>
            {
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(null, new SearchParameters
                {
                    Facets = new[] { "fundingLineId", "fundingLineException" },
                    Top = 0,
                    SearchMode = SearchMode.All,
                    IncludeTotalResultCount = true,
                    Filter = fundingLineIdFilter + $" and fundingLineException/any(x: x eq '{fundingLineId}')",
                    QueryType = QueryType.Full
                }));
            });
        }

        Task<SearchResults<ProviderCalculationResultsIndex>> BuildCalculationItemsSearchTask(
            IDictionary<string, string> facetDictionary, 
            SearchModel searchModel, 
            string calculationId)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            List<string> errorToggleWhere = !searchModel.ErrorToggle.HasValue
                ? new List<string>()
                : searchModel.ErrorToggle.Value
                    ? new List<string> { $"calculationException/any(x: x eq '{calculationId}')" }
                    : new List<string> { $"calculationException/all(x: x ne '{calculationId}')" };

            IEnumerable<string> where = facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x)).Concat(errorToggleWhere);
            return Task.Run(() =>
            {
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = (SearchMode)searchModel.SearchMode,
                    SearchFields = new List<string> { "providerName", "ukPrn", "urn", "upin", "establishmentNumber" },
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", where),
                    OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                }));
            });
        }

        Task<SearchResults<ProviderCalculationResultsIndex>> BuildFundingLineItemsSearchTask(
            IDictionary<string, string> facetDictionary,
            SearchModel searchModel,
            string fundingLineId)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            List<string> errorToggleWhere = !searchModel.ErrorToggle.HasValue
                ? new List<string>()
                : searchModel.ErrorToggle.Value
                    ? new List<string> { $"fundingLineException/any(x: x eq '{fundingLineId}')" }
                    : new List<string> { $"fundingLineException/all(x: x ne '{fundingLineId}')" };

            IEnumerable<string> where = facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x)).Concat(errorToggleWhere);
            return Task.Run(() =>
            {
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = (SearchMode)searchModel.SearchMode,
                    SearchFields = new List<string> { "providerName", "ukPrn", "urn", "upin", "establishmentNumber" },
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", where),
                    OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                }));
            });
        }

        private void ProcessSearchResults(
            SearchResults<ProviderCalculationResultsIndex> searchResult, 
            CalculationProviderResultSearchResults results, 
            string calculationId,
            string fundingLineId)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                if (_featureToggle.IsExceptionMessagesEnabled() && searchResult.Facets.Any(f => f.Name == "calculationException" || f.Name == "fundingLineException"))
                {
                    results.TotalErrorCount = (int)(searchResult?.TotalCount ?? 0);
                }
                else
                {
                    results.Facets = results.Facets.Concat(searchResult.Facets);
                }
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
                        int fundingLineIdIndex = Array.IndexOf(result.Result.CalculationId, fundingLineId);

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
                        };

                        if(calculationIdIndex >= 0)
                        {
                            calculationResult.CalculationId = calculationId;
                            calculationResult.CalculationName = result.Result.CalculationName[calculationIdIndex];
                            calculationResult.CalculationResult = result.Result.CalculationResult[calculationIdIndex].GetObjectOrNull();
                            calculationResult.CalculationExceptionType = !result.Result.CalculationExceptionType.IsNullOrEmpty() ? result.Result.CalculationExceptionType[calculationIdIndex] : string.Empty;
                            calculationResult.CalculationExceptionMessage = !result.Result.CalculationExceptionMessage.IsNullOrEmpty() ? result.Result.CalculationExceptionMessage[calculationIdIndex] : string.Empty;
                        }

                        if(fundingLineIdIndex >= 0)
                        {
                            calculationResult.FundingLineId = fundingLineId;
                            calculationResult.FundingLineName = result.Result.FundingLineName[fundingLineIdIndex];
                            calculationResult.FundingLineResult = result.Result.FundingLineResult[fundingLineIdIndex].GetValueOrNull<decimal>();
                            calculationResult.FundingLineExceptionType = !result.Result.FundingLineExceptionType.IsNullOrEmpty() ? result.Result.FundingLineExceptionType[fundingLineIdIndex] : string.Empty;
                            calculationResult.FundingLineExceptionMessage = !result.Result.FundingLineExceptionMessage.IsNullOrEmpty() ? result.Result.FundingLineExceptionMessage[fundingLineIdIndex] : string.Empty;
                        }

                        calculationResults.Add(calculationResult);
                    }
                }

                results.Results = calculationResults;
            }
        }
    }
}
