using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestRunner.Interfaces;
using Serilog;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class TestResultsRepository : ITestResultsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly ILogger _logger;
        private readonly EngineSettings _engineSettings;

        public TestResultsRepository(CosmosRepository cosmosRepository, ILogger logger, EngineSettings engineSettings)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));

            _cosmosRepository = cosmosRepository;
            _logger = logger;
            _engineSettings = engineSettings;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            var cosmosHealth = await _cosmosRepository.IsHealthOk();

            health.Name = nameof(TestResultsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = this.GetType().Name, Message = cosmosHealth.Message });

            return health;
        }

        public async Task<IEnumerable<TestScenarioResult>> GetCurrentTestResults(IEnumerable<string> providerIds, string specificationId)
        {
            Guard.ArgumentNotNull(providerIds, nameof(providerIds));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            if (providerIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<TestScenarioResult>();
            }

            ConcurrentBag<TestScenarioResult> results = new ConcurrentBag<TestScenarioResult>();

            int completedCount = 0;

            ParallelLoopResult result = Parallel.ForEach(providerIds, new ParallelOptions() { MaxDegreeOfParallelism = _engineSettings.GetCurrentProviderTestResultsDegreeOfParallelism }, async (providerId) =>
           {
               string sql = $"SELECT * FROM Root r WHERE r.documentType = \"{nameof(TestScenarioResult)}\" AND r.content.specification.id = \"{specificationId}\" AND r.content.provider.id = '{providerId}' AND r.deleted = false";
               IEnumerable<TestScenarioResult> testScenarioResults = await _cosmosRepository.QueryPartitionedEntity<TestScenarioResult>(sql, partitionEntityId: providerId);
               foreach (TestScenarioResult testScenarioResult in testScenarioResults)
               {
                   results.Add(testScenarioResult);
               }

               completedCount++;
           });

            while (completedCount < providerIds.Count())
            {
                await Task.Delay(20);
            }

            return results.AsEnumerable();
        }

        public async Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> providerResult)
        {
            Guard.ArgumentNotNull(providerResult, nameof(providerResult));

            List<TestScenarioResult> items = new List<TestScenarioResult>(providerResult);
            List<KeyValuePair<string, TestScenarioResult>> resultItems = new List<KeyValuePair<string, TestScenarioResult>>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                TestScenarioResult result = items[i];
                if (result == null)
                {
                    _logger.Error("Result {i} provided was null", i);
                    throw new InvalidOperationException($"Result {i} provided was null");
                }

                if (!result.IsValid())
                {
                    _logger.Error("Result {i} provided was not valid", i);
                    throw new InvalidOperationException($"Result {i} provided was valid");
                }

                resultItems.Add(new KeyValuePair<string, TestScenarioResult>(result.Provider.Id, result));
            }

            if (resultItems.Any())
            {
                await _cosmosRepository.BulkCreateAsync<TestScenarioResult>(resultItems, degreeOfParallelism: _engineSettings.SaveTestProviderResultsDegreeOfParallelism);
            }
            else
            {
                return HttpStatusCode.NotModified;
            }

            return HttpStatusCode.Created;
        }

        public Task<IEnumerable<DocumentEntity<TestScenarioResult>>> GetAllTestResults()
        {
            return _cosmosRepository.GetAllDocumentsAsync<TestScenarioResult>();
        }

        public async Task<ProviderTestScenarioResultCounts> GetProviderCounts(string providerId)
        {
            Task<int> passedTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>().Where(c => c.Provider.Id == providerId && c.TestResult == TestResult.Passed).Count());
            Task<int> failedTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>().Where(c => c.Provider.Id == providerId && c.TestResult == TestResult.Failed).Count());
            Task<int> ignoredTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>().Where(c => c.Provider.Id == providerId && c.TestResult == TestResult.Ignored).Count());

            await TaskHelper.WhenAllAndThrow(passedTask, failedTask, ignoredTask);

            ProviderTestScenarioResultCounts result = new ProviderTestScenarioResultCounts()
            {
                Passed = passedTask.Result,
                Failed = failedTask.Result,
                Ignored = ignoredTask.Result,
                ProviderId = providerId,
            };

            return result;
        }

        public async Task<SpecificationTestScenarioResultCounts> GetSpecificationCounts(string specificationId)
        {
            Task<int> passedTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>(enableCrossPartitionQuery: true).Where(c => c.Specification.Id == specificationId && c.TestResult == TestResult.Passed).Count());
            Task<int> failedTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>(enableCrossPartitionQuery: true).Where(c => c.Specification.Id == specificationId && c.TestResult == TestResult.Failed).Count());
            Task<int> ignoredTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>(enableCrossPartitionQuery: true).Where(c => c.Specification.Id == specificationId && c.TestResult == TestResult.Ignored).Count());

            await TaskHelper.WhenAllAndThrow(passedTask, failedTask, ignoredTask);

            return new SpecificationTestScenarioResultCounts()
            {
                Passed = passedTask.Result,
                Failed = failedTask.Result,
                Ignored = ignoredTask.Result,
                SpecificationId = specificationId,
            };
        }

        public async Task<ScenarioResultCounts> GetProvideCountForSpecification(string providerId, string specificationId)
        {
            Task<int> passedTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>().Where(c => c.Specification.Id == specificationId && c.Provider.Id == providerId && c.TestResult == TestResult.Passed).Count());
            Task<int> failedTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>().Where(c => c.Specification.Id == specificationId && c.Provider.Id == providerId && c.TestResult == TestResult.Failed).Count());
            Task<int> ignoredTask = Task.Run(() => _cosmosRepository.Query<TestScenarioResult>().Where(c => c.Specification.Id == specificationId && c.Provider.Id == providerId && c.TestResult == TestResult.Ignored).Count());

            await TaskHelper.WhenAllAndThrow(passedTask, failedTask, ignoredTask);

            return new ScenarioResultCounts()
            {
                Passed = passedTask.Result,
                Failed = failedTask.Result,
                Ignored = ignoredTask.Result,
            };
        }
    }
}
