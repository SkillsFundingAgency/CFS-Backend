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

        private static readonly FacetFilterType[] Facets = {
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

        private static readonly IEnumerable<string> DefaultOrderBy = new[] { "providerName" };

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

        public async Task<IActionResult> SearchCalculationProviderResults(SearchModel searchModel, bool useCalculationId = true)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching calculation provider results");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                CalculationProviderResultSearchResults results = await SearchCalculationProviderResultsInternal(searchModel, useCalculationId);
                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        private async Task<CalculationProviderResultSearchResults> SearchCalculationProviderResultsInternal(SearchModel searchModel, bool useCalculationId)
        {
            string entityFieldName = useCalculationId ? "calculationId" : "fundingLineId";
            string entityValue = GetSearchModelFilterValue(searchModel, entityFieldName);
            string entityExceptionFieldName = useCalculationId ? "calculationException" : "fundingLineException";

            IEnumerable <Task<SearchResults<ProviderCalculationResultsIndex>>> searchTasks = BuildSearchTasks(searchModel, entityValue, entityFieldName, entityExceptionFieldName);

            await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

            CalculationProviderResultSearchResults results = new CalculationProviderResultSearchResults();

            foreach (Task<SearchResults<ProviderCalculationResultsIndex>> searchTask in searchTasks)
            {
                ProcessSearchResults(searchTask.Result, results, entityValue, entityExceptionFieldName, useCalculationId);
            }

            return results;
        }

        private string GetSearchModelFilterValue(SearchModel searchModel, string filterName)
        {
            return searchModel.Filters != null &&
                   searchModel.Filters.ContainsKey(filterName) &&
                   searchModel.Filters[filterName].FirstOrDefault() != null
                ? searchModel.Filters[filterName].FirstOrDefault()
                : string.Empty;
        }

        IDictionary<string, string> BuildFacetDictionary(SearchModel searchModel)
        {
            searchModel.Filters ??= new Dictionary<string, string[]>();

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

        IEnumerable<Task<SearchResults<ProviderCalculationResultsIndex>>> BuildSearchTasks(SearchModel searchModel, string entityValue, string entityFieldName, string entityExceptionFieldName)
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
                            List<string> searchFields = searchModel.SearchFields?.ToList();

                            searchFields = searchFields.IsNullOrEmpty() ? new List<string>{
                                "providerName"
                            } : searchFields;
                            
                            return _searchRepositoryPolicy.ExecuteAsync(()=>
                            {
                                return _searchRepository.Search(searchModel.SearchTerm,
                                    new SearchParameters
                                    {
                                        Facets = new[]
                                        {
                                            filterPair.Key
                                        },
                                        SearchMode = (SearchMode) searchModel.SearchMode,
                                        SearchFields = searchFields,
                                        IncludeTotalResultCount = true,
                                        Filter = string.Join(" and ", facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value)),
                                        QueryType = QueryType.Full
                                    });
                            });
                        })
                    });
                }
            }

            if (facetDictionary.ContainsKey(entityFieldName) && !string.IsNullOrWhiteSpace(entityValue))
            {
                if (_featureToggle.IsExceptionMessagesEnabled())
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        BuildCalculationErrorsSearchTask(facetDictionary[entityFieldName], entityValue, entityFieldName, entityExceptionFieldName)
                    });
                }

                searchTasks = searchTasks.Concat(new[]
                {
                    BuildCalculationItemsSearchTask(facetDictionary, searchModel, entityValue, entityExceptionFieldName)
                });
            }

            return searchTasks;
        }

        private Task<SearchResults<ProviderCalculationResultsIndex>> BuildCalculationErrorsSearchTask(string entityValueFilter, string entityValue, string entityFieldName, string entityExceptionFieldName)
        {
            return Task.Run(() =>
            {
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(null, new SearchParameters
                {
                    Facets = new[] { entityFieldName, entityExceptionFieldName },
                    Top = 0,
                    SearchMode = SearchMode.All,
                    IncludeTotalResultCount = true,
                    Filter = entityValueFilter + $" and {entityExceptionFieldName}/any(x: x eq '{entityValue}')",
                    QueryType = QueryType.Full
                }));
            });
        }

        private Task<SearchResults<ProviderCalculationResultsIndex>> BuildCalculationItemsSearchTask(
            IDictionary<string, string> facetDictionary, 
            SearchModel searchModel, 
            string entityValue,
            string entityExceptionFieldName)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            List<string> errorToggleWhere = !searchModel.ErrorToggle.HasValue || string.IsNullOrWhiteSpace(entityValue)
                ? new List<string>()
                : searchModel.ErrorToggle.Value
                    ? new List<string> { $"{entityExceptionFieldName}/any(x: x eq '{entityValue}')" }
                    : new List<string> { $"{entityExceptionFieldName}/all(x: x ne '{entityValue}')" };

            IEnumerable<string> where = facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x)).Concat(errorToggleWhere);
            return Task.Run(() =>
            {
                List<string> searchFields = searchModel.SearchFields?.ToList();

                searchFields = searchFields.IsNullOrEmpty() ? new List<string>{
                    "providerName", "ukPrn", "urn", "upin", "establishmentNumber"
                } : searchFields;
                
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = (SearchMode)searchModel.SearchMode,
                    SearchFields = searchFields,
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
            string entityValue,
            string entityExceptionFieldName,
            bool useCalculationId)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                if (_featureToggle.IsExceptionMessagesEnabled() && searchResult.Facets.Any(f => f.Name == entityExceptionFieldName))
                {
                    results.TotalErrorCount = (int)(searchResult.TotalCount ?? 0);
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
                        int valueIndex = Array.IndexOf(useCalculationId ? result.Result.CalculationId : result.Result.FundingLineId, entityValue);

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
                            IsIndicativeProvider = result.Result.IsIndicativeProvider
                        };

                        if(valueIndex >= 0)
                        {
                            if (useCalculationId)
                            {
                                calculationResult.CalculationId = entityValue;
                                calculationResult.CalculationName = result.Result.CalculationName[valueIndex];
                                calculationResult.CalculationResult = result.Result.CalculationResult[valueIndex].GetObjectOrNull();
                                calculationResult.CalculationExceptionType = !result.Result.CalculationExceptionType.IsNullOrEmpty() ? result.Result.CalculationExceptionType[valueIndex] : string.Empty;
                                calculationResult.CalculationExceptionMessage = !result.Result.CalculationExceptionMessage.IsNullOrEmpty() ? result.Result.CalculationExceptionMessage[valueIndex] : string.Empty;
                            }
                            else
                            {
                                calculationResult.FundingLineId = entityValue;
                                calculationResult.FundingLineName = result.Result.FundingLineName[valueIndex];
                                calculationResult.FundingLineResult = result.Result.FundingLineResult[valueIndex].GetValueOrNull<decimal>();
                                calculationResult.FundingLineExceptionType = !result.Result.FundingLineExceptionType.IsNullOrEmpty() ? result.Result.FundingLineExceptionType[valueIndex] : string.Empty;
                                calculationResult.FundingLineExceptionMessage = !result.Result.FundingLineExceptionMessage.IsNullOrEmpty() ? result.Result.FundingLineExceptionMessage[valueIndex] : string.Empty;
                            }
                        }

                        calculationResults.Add(calculationResult);
                    }
                }

                results.Results = calculationResults;
            }
        }
    }
}
