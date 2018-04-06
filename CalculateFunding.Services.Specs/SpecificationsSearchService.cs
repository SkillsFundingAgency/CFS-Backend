using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsSearchService : ISpecificationsSearchService
    {
        private IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };

        private readonly ISearchRepository<SpecificationIndex> _searchRepository;
        private readonly ILogger _logger;

        public SpecificationsSearchService(ISearchRepository<SpecificationIndex> searchRepository, ILogger logger)
        {
            _searchRepository = searchRepository;
            _logger = logger;
        }

        async public Task<IActionResult> SearchSpecifications(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SearchModel searchModel = JsonConvert.DeserializeObject<SearchModel>(json);

            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching specifications");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                SearchResults<SpecificationIndex> searchResults = await PerformSearch(searchModel);

                SpecificationSearchResults results = new SpecificationSearchResults
                {
                    TotalCount = (int)(searchResults?.TotalCount ?? 0),
                    Results = searchResults.Results?.Select(m => new SpecificationSearchResult
                    {
                        SpecificationId = m.Result.Id,
                        SpecificationName = m.Result.Name,
                        DefinitionRelationshipCount = m.Result.DataDefinitionRelationshipIds.IsNullOrEmpty()
                            ? 0 : m.Result.DataDefinitionRelationshipIds.Count()
                    })
                };

                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        Task<SearchResults<SpecificationIndex>> PerformSearch(SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            
            return Task.Run(() =>
            {
                return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = SearchMode.Any,
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", searchModel.Filters.Where(m => !string.IsNullOrWhiteSpace(m.Value.FirstOrDefault())).Select(m => $"({m.Key} eq '{m.Value.First()}')")),
                    OrderBy = searchModel.OrderBy.IsNullOrEmpty() ? DefaultOrderBy.ToList() : searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                });
            });
        }
    }
}
