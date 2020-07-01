using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsSearchService : 
        SearchService<SpecificationIndex>,  ISpecificationsSearchService, IHealthChecker
    {
        private readonly ILogger _logger;

        private static readonly FacetFilterType[] SpecificationIndexFacets = {
            new FacetFilterType("status"),
            new FacetFilterType("fundingPeriodName"),
            new FacetFilterType("fundingStreamNames", true)
        };
        
        private static readonly FacetFilterType[] DatasetRelationshipFacets = {
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
            (bool Ok, string Message) = await SearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(SpecificationsService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = SearchRepository.GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        public async Task<IActionResult> SearchSpecificationDatasetRelationships(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching specifications");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                searchModel.OrderBy = DefaultOrderBy;
                
                IEnumerable<Task<SearchResults<SpecificationIndex>>> searchTasks = await BuildSearchTasks(searchModel, DatasetRelationshipFacets);

                if (searchTasks.IsNullOrEmpty())
                {
                    return new InternalServerErrorResult("Failed to build search tasks");
                }

                SpecificationDatasetRelationshipsSearchResults results = new SpecificationDatasetRelationshipsSearchResults();

                await TaskHelper.WhenAllAndThrow(searchTasks.ToArray());

                foreach (Task<SearchResults<SpecificationIndex>> searchTask in searchTasks)
                {
                    SearchResults<SpecificationIndex> searchResult = searchTask.Result;

                    if (!searchResult.Facets.IsNullOrEmpty())
                    {
                        results.Facets = searchResult.Facets.Concat(searchResult.Facets);
                    }
                    else
                    {
                        results.TotalCount = (int)(searchResult.TotalCount ?? 0);
                        results.Results = searchResult.Results?.Select(m => new SpecificationDatasetRelationshipsSearchResult
                        {
                            SpecificationId = m.Result.Id,
                            SpecificationName = m.Result.Name,
                            FundingPeriodName = m.Result.FundingPeriodName,
                            FundingStreamNames = m.Result.FundingStreamNames,
                            TotalMappedDataSets = m.Result.TotalMappedDataSets.GetValueOrDefault(),
                            MapDatasetLastUpdated = m.Result.MapDatasetLastUpdated,
                            DefinitionRelationshipCount = m.Result.DataDefinitionRelationshipIds.IsNullOrEmpty()
                                ? 0
                                : m.Result.DataDefinitionRelationshipIds.Count()
                        });
                    }
                }

                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> SearchSpecifications(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching specifications");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                searchModel.OrderBy = DefaultOrderBy;

                IEnumerable<Task<SearchResults<SpecificationIndex>>> searchTasks = await BuildSearchTasks(searchModel, SpecificationIndexFacets);

                if (searchTasks.IsNullOrEmpty())
                {
                    return new InternalServerErrorResult("Failed to build search tasks");
                }

                SpecificationSearchResults results = new SpecificationSearchResults();

                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

                foreach (Task<SearchResults<SpecificationIndex>> searchTask in searchTasks)
                {
                    SearchResults<SpecificationIndex> searchResult = searchTask.Result;

                    if (!searchResult.Facets.IsNullOrEmpty())
                    {
                        results.Facets = results.Facets.Concat(searchResult.Facets);
                    }
                    else
                    {
                        results.TotalCount = (int)(searchResult.TotalCount ?? 0);
                        results.Results = searchResult.Results?.Select(m => new SpecificationSearchResult
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
    }
}
