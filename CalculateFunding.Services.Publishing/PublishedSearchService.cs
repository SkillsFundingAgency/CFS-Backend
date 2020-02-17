using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Filtering;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using Serilog;


namespace CalculateFunding.Services.Publishing
{
    public class PublishedSearchService : SearchService<PublishedProviderIndex>, IPublishedSearchService, IHealthChecker
    {
        private static readonly FacetFilterType[] Facets = {
            new FacetFilterType("providerType"),
            new FacetFilterType("localAuthority"),
            new FacetFilterType("fundingStatus")
        };

        private readonly ILogger _logger;

        public PublishedSearchService(ISearchRepository<PublishedProviderIndex> searchRepository, ILogger logger)
            : base(searchRepository)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            
            _logger = logger;
        }

        public async Task<IActionResult> SearchPublishedProviderLocalAuthorities(
            string searchText,
            string fundingStreamId,
            string fundingPeriodId)
        {
            string facetName = "localAuthority";

            FilterHelper filterHelper = new FilterHelper();
            AddFiltersForNotification(fundingStreamId, fundingPeriodId, filterHelper);

            SearchResults<PublishedProviderIndex> searchResults = await Task.Run(() =>
            {
                return SearchRepository.Search(string.Empty, new SearchParameters
                {
                    Facets = new[] { $"{facetName},count:1000" },
                    Top = 0,
                    Filter = filterHelper.BuildAndFilterQuery()
                });
            });

            IEnumerable<string> distinctFacetValues = searchResults
                .Facets
                .SingleOrDefault(x => x.Name == facetName)
                .FacetValues
                .Where(x => x.Name?.Split().Any(s=> 
                { 
                    return searchText != null ? s.ToLowerInvariant().StartsWith(searchText.ToLowerInvariant()) : true; 
                }) == true)
                .Select(x => x.Name);

            return new OkObjectResult(distinctFacetValues);
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var searchRepoHealth = await SearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = SearchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> SearchPublishedProviders(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching published providers");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                if (searchModel.OrderBy.IsNullOrEmpty())
                {
                    searchModel.OrderBy = new[] { "providerName" };
                }

                IEnumerable<Task<SearchResults<PublishedProviderIndex>>> searchTasks = await BuildSearchTasks(searchModel, Facets);

                if (searchTasks.IsNullOrEmpty())
                {
                    return new InternalServerErrorResult("Failed to build search tasks");
                }

                PublishedSearchResults results = new PublishedSearchResults();

                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

                foreach (var searchTask in searchTasks)
                {
                    SearchResults<PublishedProviderIndex> searchResult = searchTask.Result;

                    if (!searchResult.Facets.IsNullOrEmpty())
                    {
                        results.Facets = results.Facets.Concat(searchResult.Facets);
                    }
                    else
                    {
                        results.TotalCount = (int)(searchResult?.TotalCount ?? 0);
                        results.Results = searchResult?.Results?.Select(m => new PublishedSearchResult
                        {
                            Id = m.Result.Id,
                            ProviderType = m.Result.ProviderType,
                            LocalAuthority = m.Result.LocalAuthority,
                            FundingStatus = m.Result.FundingStatus,
                            ProviderName = m.Result.ProviderName,
                            UKPRN = m.Result.UKPRN,
                            FundingValue = m.Result.FundingValue,
                            SpecificationId = m.Result.SpecificationId,
                            FundingStreamId = m.Result.FundingStreamId,
                            FundingPeriodId = m.Result.FundingPeriodId
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

        private static void AddFiltersForNotification(string fundingStreamId, string fundingPeriodId, FilterHelper filterHelper)
        {
            if (!fundingStreamId.IsNullOrEmpty())
            {
                filterHelper.Filters.Add(new Filter("fundingStreamId", new[] { fundingStreamId }, false, "eq"));
            }

            if (!fundingPeriodId.IsNullOrEmpty())
            {
                filterHelper.Filters.Add(new Filter("fundingPeriodId", new[] { fundingPeriodId }, false, "eq"));
            }
        }
    }
}
