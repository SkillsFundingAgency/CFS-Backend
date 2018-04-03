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

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class TestResultsRepository : ITestResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public TestResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
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

        public Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> providerResult)
        {
            throw new NotImplementedException();
        }
    }
}
