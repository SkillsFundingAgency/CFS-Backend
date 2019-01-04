using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsSearchService : SearchService<SpecificationIndex>,  ISpecificationsSearchService, IHealthChecker
    {
        private readonly ILogger _logger;

        private FacetFilterType[] Facets = {
            new FacetFilterType("status"),
            new FacetFilterType("fundingPeriodName"),
            new FacetFilterType("fundingStreamNames", true)
        };

        public SpecificationsSearchService(ISearchRepository<SpecificationIndex> searchRepository, ILogger logger)
            : base(searchRepository)
        {
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var searchRepoHealth = await SearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(SpecificationsService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = SearchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        async public Task<IActionResult> SearchSpecificationDatasetRelationships(HttpRequest request)
        {
            SearchModel searchModel = await GetSearchModelFromRequest(request);

            if (searchModel == null )
            {
                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                searchModel.OrderBy = DefaultOrderBy;

                SearchResults<SpecificationIndex> searchResults = await PerformNonfacetSearch(searchModel);

                SpecificationDatasetRelationshipsSearchResults results = new SpecificationDatasetRelationshipsSearchResults
                {
                    TotalCount = (int)(searchResults?.TotalCount ?? 0),
                    Results = searchResults.Results?.Select(m => new SpecificationDatasetRelationshipsSearchResult
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

        async public Task<IActionResult> SearchSpecifications(HttpRequest request)
        {
            SearchModel searchModel = await GetSearchModelFromRequest(request);

            if (searchModel == null)
            {
                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                searchModel.OrderBy = DefaultOrderBy;

                IEnumerable<Task<SearchResults<SpecificationIndex>>> searchTasks = await BuildSearchTasks(searchModel, Facets);

                if (searchTasks.IsNullOrEmpty())
                {
                    return new InternalServerErrorResult("Failed to build search tasks");
                }

                SpecificationSearchResults results = new SpecificationSearchResults();

                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

                foreach (var searchTask in searchTasks)
                {
                    SearchResults<SpecificationIndex> searchResult = searchTask.Result;

                    if (!searchResult.Facets.IsNullOrEmpty())
                    {
                        results.Facets = results.Facets.Concat(searchResult.Facets);
                    }
                    else
                    {
                        results.TotalCount = (int)(searchResult?.TotalCount ?? 0);
                        results.Results = searchResult?.Results?.Select(m => new SpecificationSearchResult
                        {
                            Id = m.Result.Id,
                            Name = m.Result.Name,
                            FundingPeriodName = m.Result.FundingPeriodName,
                            FundingStreamNames = m.Result.FundingStreamNames,
                            Status = m.Result.Status,
                            LastUpdatedDate = m.Result.LastUpdatedDate.ToNullableLocal(),
                            Description = m.Result.Description
                        });
                    }
                }

                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new InternalServerErrorResult($"Failed to query search, with exception: {exception.Message}");
            }
        }

        async Task<SearchModel> GetSearchModelFromRequest(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SearchModel searchModel = JsonConvert.DeserializeObject<SearchModel>(json);

            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching specifications");

                return null;
            }

            return searchModel;
        }
    }
}
