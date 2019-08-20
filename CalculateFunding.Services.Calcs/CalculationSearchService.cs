using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.Core.Filtering;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationSearchService : ICalculationsSearchService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;


        private IEnumerable<string> DefaultOrderBy = new[] { "name" };

        public CalculationSearchService(ILogger logger,
            ISearchRepository<CalculationIndex> searchRepository)
        {
            _logger = logger;
            _searchRepository = searchRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var searchRepoHealth = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> SearchCalculations(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SearchModel searchModel = JsonConvert.DeserializeObject<SearchModel>(json);

            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching calculations");

                return new BadRequestObjectResult("An invalid search model was provided");
            } 

            try
            {
                SearchResults<CalculationIndex> items = await BuildItemsSearchTask(searchModel);
                CalculationSearchResults results = new CalculationSearchResults();

	           
		       ProcessSearchResults(items, results);
	            

	            return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        Task<SearchResults<CalculationIndex>> BuildItemsSearchTask(SearchModel searchModel)
        {
            IDictionary<string, string[]> searchModelDictionary = searchModel.Filters;

            List<Filter> filters = searchModelDictionary?.Select(keyValueFilterPair => new Filter(keyValueFilterPair.Key, keyValueFilterPair.Value, false, "eq")).ToList();

            FilterHelper filterHelper = new FilterHelper(filters);

            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
			return Task.Run(() =>
			{
				return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
				{
					Skip = skip,
					Top = searchModel.Top,
					SearchMode = (SearchMode)searchModel.SearchMode,
					SearchFields = new List<string> { "name" },
					IncludeTotalResultCount = true,
                    Filter = filterHelper.BuildAndFilterQuery(),
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
                    LastUpdatedDate = m.Result.LastUpdatedDate
                });
            }
        }
    }
}
