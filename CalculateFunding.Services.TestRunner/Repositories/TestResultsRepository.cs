using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Services.Core.Helpers;
using Serilog;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class TestResultsRepository : ITestResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly ILogger _logger;

        public TestResultsRepository(CosmosRepository cosmosRepository, ILogger logger)
        {
            _cosmosRepository = cosmosRepository;
            _logger = logger;
        }

        public Task<IEnumerable<TestScenarioResult>> GetCurrentTestResults(IEnumerable<string> providerIds, string specificationId)
        {
            Guard.ArgumentNotNull(providerIds, nameof(providerIds));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string providerIdList = string.Join(",", providerIds.Select(m => $"\"{m}\""));

            string sql = $"SELECT * FROM Root r where r.documentType = \"{nameof(TestScenarioResult)}\" and r.content.specification.id = \"{specificationId}\" and r.content.provider.id in ({providerIdList})";

            IQueryable<TestScenarioResult> results = _cosmosRepository.RawQuery<TestScenarioResult>(sql);

            return Task.FromResult(results.AsEnumerable());
        }

        public async Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> providerResult)
        {
            Guard.ArgumentNotNull(providerResult, nameof(providerResult));

            List<TestScenarioResult> items = new List<TestScenarioResult>(providerResult);
            for(int i = 0; i < items.Count; i++)
            {
                TestScenarioResult result = items[i];
                if(result == null)
                {
                    _logger.Error("Result {i} provided was null", i);
                    throw new InvalidOperationException($"Result {i} provided was null");
                }

                if (!result.IsValid())
                {
                    _logger.Error("Result {i} provided was not valid", i);
                    throw new InvalidOperationException($"Result {i} provided was valid");
                }
            }

            if (items.Any())
            {
                await _cosmosRepository.BulkCreateAsync<TestScenarioResult>(items);
            }
            else
            {
                return HttpStatusCode.NoContent;
            }

            return HttpStatusCode.Created;
        }
    }
}
