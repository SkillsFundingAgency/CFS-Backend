using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;


namespace CalculateFunding.Services.Publishing
{
    public class PublishedSearchService : SearchService<PublishedIndex>, IPublishedSearchService, IHealthChecker
    {
        private readonly ILogger _logger;

        private FacetFilterType[] Facets = {
            new FacetFilterType("providerType"),
            new FacetFilterType("localAuthority"),
            new FacetFilterType("fundingStatus", true)
        };

        public PublishedSearchService(ISearchRepository<PublishedIndex> searchRepository, ILogger logger)
            : base(searchRepository)
        {
            _logger = logger;
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

        public async Task<IActionResult> SearchPublishedProviders(HttpRequest request)
        {
            SearchModel searchModel = await GetSearchModelFromRequest(request);


            if (searchModel == null)
            {
                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                searchModel.OrderBy = new[] { "providerName desc" }; ;

                IEnumerable<Task<SearchResults<PublishedIndex>>> searchTasks = await BuildSearchTasks(searchModel, Facets);

                if (searchTasks.IsNullOrEmpty())
                {
                    return new InternalServerErrorResult("Failed to build search tasks");
                }

                PublishedSearchResults results = new PublishedSearchResults();

                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

                foreach (var searchTask in searchTasks)
                {
                    SearchResults<PublishedIndex> searchResult = searchTask.Result;

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
                            FundingStreamIds = m.Result.FundingStreamIds,
                            FundingPeriodId = m.Result.FundingPeriodId

                        }) ;
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
                _logger.Error("A null or invalid search model was provided for searching published providers");

                return null;
            }

            return searchModel;
        }
    }
}
