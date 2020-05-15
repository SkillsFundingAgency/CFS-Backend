using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using System.Linq;
using CalculateFunding.Models;
using CalculateFunding.Repositories.Common.Search.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Serilog;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public class TemplateSearchService : IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<TemplateIndex> _searchRepository;

        private readonly FacetFilterType[] _facets = {
            new FacetFilterType("fundingStreamId"),
            new FacetFilterType("fundingStreamName"),
            new FacetFilterType("fundingPeriodId"),
            new FacetFilterType("fundingPeriodName")
        };

        private readonly IEnumerable<string> _defaultOrderBy = new[] { "lastUpdatedDate desc" };

        public TemplateSearchService(ILogger logger,
            ISearchRepository<TemplateIndex> searchRepository)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _logger = logger;
            _searchRepository = searchRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(TemplateSearchService)
            };

            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> SearchTemplates(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching templates");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                IEnumerable<SearchResults<TemplateIndex>> searchResults = await GetSearchResults(searchModel);

                TemplateSearchResults results = new TemplateSearchResults();
                foreach (SearchResults<TemplateIndex> searchResult in searchResults)
                {
                    ProcessSearchResults(searchResult, results);
                }

                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        private IDictionary<string, string> BuildFacetDictionary(SearchModel searchModel)
        {
            if (searchModel.Filters == null)
            {
                searchModel.Filters = new Dictionary<string, string[]>();
            }

            searchModel.Filters = searchModel.Filters.ToList()
                .Where(m => !m.Value.IsNullOrEmpty())
                .ToDictionary(m => m.Key, m => m.Value);

            IDictionary<string, string> facetDictionary = new Dictionary<string, string>();

            foreach (var facet in _facets)
            {
                string filter = "";
                if (searchModel.Filters.ContainsKey(facet.Name) && searchModel.Filters[facet.Name].AnyWithNullCheck())
                {
                    filter = facet.IsMulti ? $"({facet.Name}/any(x: {string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"x eq '{x}'"))}))" :
                        $"({string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"{facet.Name} eq '{x}'"))})";
                }
                facetDictionary.Add(facet.Name, filter);
            }

            return facetDictionary;
        }

        private async Task<IEnumerable<SearchResults<TemplateIndex>>> GetSearchResults(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            List<SearchResults<TemplateIndex>> searchResults = new List<SearchResults<TemplateIndex>>();

            if (searchModel.IncludeFacets)
            {
                foreach (KeyValuePair<string, string> filterPair in facetDictionary)
                {
                    IEnumerable<string> facets = facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value);

                    SearchResults<TemplateIndex> searchResult = await _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                    {
                        Facets = new[] { filterPair.Key },
                        SearchMode = (SearchMode)searchModel.SearchMode,
                        IncludeTotalResultCount = true,
                        Filter = string.Join(" and ", facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value)),
                        QueryType = QueryType.Full
                    });

                    searchResults.Add(searchResult);
                }
            }

            SearchResults<TemplateIndex> itemSearch = await GetItemSearchResult(facetDictionary, searchModel);
            searchResults.Add(itemSearch);

            return searchResults;
        }

        private async Task<SearchResults<TemplateIndex>> GetItemSearchResult(IDictionary<string, string> facetDictionary, SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;

            return await _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
            {
                Skip = skip,
                Top = searchModel.Top,
                SearchMode = (SearchMode)searchModel.SearchMode,
                IncludeTotalResultCount = true,
                Filter = string.Join(" and ", facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? _defaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                QueryType = QueryType.Full
            });
        }

        private static void ProcessSearchResults(SearchResults<TemplateIndex> searchResult, TemplateSearchResults results)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                results.TotalCount = (int)(searchResult.TotalCount ?? 0);
                results.Results = searchResult.Results?.Select(m => new TemplateSearchResult
                {
                    Id = m.Result.Id,
                    Name = m.Result.Name,
                    FundingPeriodId = m.Result.FundingPeriodId,
                    FundingPeriodName = m.Result.FundingPeriodName,
                    FundingStreamId = m.Result.FundingStreamId,
                    FundingStreamName = m.Result.FundingStreamName,
                    LastUpdatedDate = m.Result.LastUpdatedDate.LocalDateTime,
                    LastUpdatedAuthorId = m.Result.LastUpdatedAuthorId,
                    LastUpdatedAuthorName = m.Result.LastUpdatedAuthorName,
                    Version = m.Result.Version,
                    CurrentMajorVersion = m.Result.CurrentMajorVersion,
                    CurrentMinorVersion = m.Result.CurrentMinorVersion,
                    PublishedMajorVersion = m.Result.PublishedMajorVersion,
                    PublishedMinorVersion = m.Result.PublishedMinorVersion,
                    HasReleasedVersion = m.Result.HasReleasedVersion
                });
            }
        }
    }
}
