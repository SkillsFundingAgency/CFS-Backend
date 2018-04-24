using CalculateFunding.Models;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios
{
    public class ScenariosSearchService : IScenariosSearchService
    {
        private IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };
        private readonly ILogger _logger;
        private readonly ISearchRepository<ScenarioIndex> _searchRepository;
        private readonly IScenariosRepository _scenariosRepository;

        public ScenariosSearchService(ISearchRepository<ScenarioIndex> searchRepository, IScenariosRepository scenariosRepository, ILogger logger)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(scenariosRepository, nameof(scenariosRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _searchRepository = searchRepository;
            _scenariosRepository = scenariosRepository;
            _logger = logger;
        }

        async public Task<IActionResult> SearchScenarios(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SearchModel searchModel = JsonConvert.DeserializeObject<SearchModel>(json);

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
                        PeriodName = m.Result.PeriodName,
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

        public async Task<IActionResult> ReIndex(HttpRequest request)
        {
            IEnumerable<DocumentEntity<TestScenario>> testScenarios = await _scenariosRepository.GetAllTestScenarios();
            List<ScenarioIndex> testScenarioIndexes = new List<ScenarioIndex>();

            foreach (DocumentEntity<TestScenario> entity in testScenarios)
            {
                TestScenario testScenario = entity.Content;

                testScenarioIndexes.Add(new ScenarioIndex()
                {
                    Id = testScenario.Id,
                    Name = testScenario.Name,
                    Description = testScenario.Description,
                    LastUpdatedDate = entity.UpdatedAt,
                    FundingStreamId = testScenario?.FundingStream.Id,
                    FundingStreamName = testScenario?.FundingStream.Name,
                    PeriodId = testScenario?.Period.Id,
                    PeriodName = testScenario?.Period.Name,
                    SpecificationId = testScenario.Specification.Id,
                    SpecificationName = testScenario.Specification.Name,
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
