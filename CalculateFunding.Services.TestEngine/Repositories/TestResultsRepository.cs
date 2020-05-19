using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestRunner.Interfaces;
using Serilog;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class TestResultsRepository : ITestResultsRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ILogger _logger;
        private readonly EngineSettings _engineSettings;

        public TestResultsRepository(ICosmosRepository cosmosRepository, ILogger logger, EngineSettings engineSettings)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));

            _cosmosRepository = cosmosRepository;
            _logger = logger;
            _engineSettings = engineSettings;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            var (Ok, Message) = _cosmosRepository.IsHealthOk();

            health.Name = nameof(TestResultsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = GetType().Name, Message = Message });

            return Task.FromResult(health);
        }

        public async Task DeleteCurrentTestScenarioTestResults(IEnumerable<TestScenarioResult> testScenarioResults)
        {
            Guard.ArgumentNotNull(testScenarioResults, nameof(testScenarioResults));

            await _cosmosRepository.BulkDeleteAsync<TestScenarioResult>(
                entities: testScenarioResults.Select(x => new KeyValuePair<string, TestScenarioResult>(x.Provider.Id, x)),
                degreeOfParallelism: 15,
                hardDelete: true
            );
        }

        public async Task<IEnumerable<TestScenarioResult>> GetCurrentTestResults(IEnumerable<string> providerIds, string specificationId)
        {
            Guard.ArgumentNotNull(providerIds, nameof(providerIds));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            if (providerIds.IsNullOrEmpty()) return Enumerable.Empty<TestScenarioResult>();

            ConcurrentBag<TestScenarioResult> results = new ConcurrentBag<TestScenarioResult>();

            int completedCount = 0;

            ParallelLoopResult result = Parallel.ForEach(providerIds, new ParallelOptions() { MaxDegreeOfParallelism = _engineSettings.GetCurrentProviderTestResultsDegreeOfParallelism }, async (providerId) =>
            {
                try
                {
                    CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
                    {
                        QueryText = @"SELECT * 
                                FROM    Root r 
                                WHERE   r.documentType = @DocumentType 
                                        AND r.content.specification.id = @SpecificationId
                                        AND r.deleted = false",
                        Parameters = new[]
                        {
                            new CosmosDbQueryParameter("@DocumentType", nameof(TestScenarioResult)),
                            new CosmosDbQueryParameter("@SpecificationId", specificationId)
                        }
                    };

                    IEnumerable<TestScenarioResult> testScenarioResults = await _cosmosRepository.QueryPartitionedEntity<TestScenarioResult>(cosmosDbQuery, partitionKey: providerId);
                    foreach (TestScenarioResult testScenarioResult in testScenarioResults)
                    {
                        results.Add(testScenarioResult);
                    }
                }
                finally
                {
                    completedCount++;
                }
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
                await _cosmosRepository.BulkUpsertAsync<TestScenarioResult>(resultItems, degreeOfParallelism: _engineSettings.SaveTestProviderResultsDegreeOfParallelism);
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
            Task<int> passedTask = Task.Run(async() => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Provider.Id == providerId && c.Content.TestResult == TestResult.Passed)).Count());
            Task<int> failedTask = Task.Run(async () => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Provider.Id == providerId && c.Content.TestResult == TestResult.Failed)).Count());
            Task<int> ignoredTask = Task.Run(async () => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Provider.Id == providerId && c.Content.TestResult == TestResult.Ignored)).Count());

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
            Task<int> passedTask = Task.Run(async() => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Specification.Id == specificationId && c.Content.TestResult == TestResult.Passed)).Count());
            Task<int> failedTask = Task.Run(async() => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Specification.Id == specificationId && c.Content.TestResult == TestResult.Failed)).Count());
            Task<int> ignoredTask = Task.Run(async() => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Specification.Id == specificationId && c.Content.TestResult == TestResult.Ignored)).Count());

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
            Task<int> passedTask = Task.Run(async() => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Specification.Id == specificationId && c.Content.Provider.Id == providerId && c.Content.TestResult == TestResult.Passed)).Count());
            Task<int> failedTask = Task.Run(async() => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Specification.Id == specificationId && c.Content.Provider.Id == providerId && c.Content.TestResult == TestResult.Failed)).Count());
            Task<int> ignoredTask = Task.Run(async() => (await _cosmosRepository.Query<TestScenarioResult>(c => c.Content.Specification.Id == specificationId && c.Content.Provider.Id == providerId && c.Content.TestResult == TestResult.Ignored)).Count());

            await TaskHelper.WhenAllAndThrow(passedTask, failedTask, ignoredTask);

            return new ScenarioResultCounts()
            {
                Passed = passedTask.Result,
                Failed = failedTask.Result,
                Ignored = ignoredTask.Result,
            };
        }

        public async Task DeleteTestResultsBySpecificationId(string specificationId, DeletionType deletionType)
        {
            IEnumerable<TestScenarioResult> testResults = await GetTestResultsBySpecificationId(specificationId);

            List<TestScenarioResult> testResultList = testResults.ToList();

            if (!testResultList.Any())
                return;

            if (deletionType == DeletionType.SoftDelete)
                await _cosmosRepository.BulkDeleteAsync(testResultList.ToDictionary(c => c.Id), hardDelete:false);
            if (deletionType == DeletionType.PermanentDelete)
                await _cosmosRepository.BulkDeleteAsync(testResultList.ToDictionary(c => c.Id), hardDelete:true);
        }

        private async Task<IEnumerable<TestScenarioResult>> GetTestResultsBySpecificationId(string specificationId)
        {
            return await _cosmosRepository.Query<TestScenarioResult>(x => x.Content.Specification.Id == specificationId);
        }
    }
}
