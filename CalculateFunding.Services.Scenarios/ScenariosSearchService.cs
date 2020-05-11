using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Polly;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Scenarios
{
    public class ScenariosSearchService : IScenariosSearchService, IHealthChecker
    {
        private IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };
        private readonly ILogger _logger;
        private readonly ISearchRepository<ScenarioIndex> _searchRepository;
        private readonly IScenariosRepository _scenariosRepository;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly AsyncPolicy _specificationsApiClientPolicy;

        public ScenariosSearchService(
            ISearchRepository<ScenarioIndex> searchRepository,
            IScenariosRepository scenariosRepository,
            ISpecificationsApiClient specificationsApiClient,
            ILogger logger,
            IScenariosResiliencePolicies scenariosResiliencePolicies)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(scenariosRepository, nameof(scenariosRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scenariosResiliencePolicies, nameof(scenariosResiliencePolicies));
            Guard.ArgumentNotNull(scenariosResiliencePolicies?.SpecificationsApiClient, nameof(scenariosResiliencePolicies.SpecificationsApiClient));

            _searchRepository = searchRepository;
            _scenariosRepository = scenariosRepository;
            _specificationsApiClient = specificationsApiClient;
            _logger = logger;
            _specificationsApiClientPolicy = scenariosResiliencePolicies.SpecificationsApiClient;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var searchRepoHealth = await _searchRepository.IsHealthOk();
            ServiceHealth scenariosRepoHealth = await ((IHealthChecker)_scenariosRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ScenariosService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.AddRange(scenariosRepoHealth.Dependencies);

            return health;
        }

        async public Task<IActionResult> SearchScenarios(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching scenarios");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                SearchResults<ScenarioIndex> searchResults = await PerformSearch(searchModel);

                ScenarioSearchResults results = new ScenarioSearchResults
                {
                    TotalCount = (int)(searchResults?.TotalCount ?? 0),
                    Results = searchResults.Results?.Select(m => new ScenarioSearchResult
                    {
                        Id = m.Result.Id,
                        Name = m.Result.Name,
                        Description = m.Result.Description,
                        SpecificationName = m.Result.SpecificationName,
                        FundingPeriodName = m.Result.FundingPeriodName,
                        Status = m.Result.Status,
                        LastUpdatedDate = m.Result.LastUpdatedDate
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

        public async Task<IActionResult> ReIndex()
        {
            IEnumerable<DocumentEntity<TestScenario>> testScenarios = await _scenariosRepository.GetAllTestScenarios();
            List<ScenarioIndex> testScenarioIndexes = new List<ScenarioIndex>();

            Dictionary<string, SpecModel.SpecificationSummary> specifications = new Dictionary<string, SpecModel.SpecificationSummary>();

            foreach (DocumentEntity<TestScenario> entity in testScenarios)
            {
                TestScenario testScenario = entity.Content;

                SpecModel.SpecificationSummary specificationSummary = null;
                if (!specifications.ContainsKey(testScenario.SpecificationId))
                {
                    Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(testScenario.SpecificationId));

                    specificationSummary = specificationApiResponse.Content;

                    specifications.Add(testScenario.SpecificationId, specificationSummary);
                }
                else
                {
                    specificationSummary = specifications[testScenario.SpecificationId];
                }

                testScenarioIndexes.Add(new ScenarioIndex()
                {
                    Id = testScenario.Id,
                    Name = testScenario.Name,
                    Description = testScenario.Current.Description,
                    LastUpdatedDate = entity.UpdatedAt,
                    FundingStreamIds = testScenario.Current?.FundingStreamIds.ToArray(),
                    FundingStreamNames = specificationSummary.FundingStreams.Select(s => s.Name).ToArray(),
                    FundingPeriodId = testScenario.Current?.FundingPeriodId,
                    FundingPeriodName = specificationSummary.FundingPeriod.Name,
                    SpecificationId = testScenario.SpecificationId,
                    SpecificationName = specificationSummary.Name,
                    Status = Enum.GetName(typeof(PublishStatus), testScenario.Current.PublishStatus),
                });
            }

            await _searchRepository.Index(testScenarioIndexes);

            return new OkObjectResult($"Updated {testScenarioIndexes.Count} records");
        }

        Task<SearchResults<ScenarioIndex>> PerformSearch(SearchModel searchModel)
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
