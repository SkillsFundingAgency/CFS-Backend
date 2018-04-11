using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Services.Core.Helpers;
using Serilog;
using CalculateFunding.Services.Core.Options;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class TestResultsRepository : ITestResultsRepository
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

        public async Task<IEnumerable<TestScenarioResult>> GetCurrentTestResults(IEnumerable<string> providerIds, string specificationId)
        {
            Guard.ArgumentNotNull(providerIds, nameof(providerIds));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            if (providerIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<TestScenarioResult>();
            }


            List<Task<IEnumerable<TestScenarioResult>>> queryTasks = new List<Task<IEnumerable<TestScenarioResult>>>(providerIds.Count());
            foreach (string providerId in providerIds)
            {
                string sql = $"SELECT * FROM Root r WHERE r.documentType = \"{nameof(TestScenarioResult)}\" AND r.content.specification.id = \"{specificationId}\" AND r.content.provider.id = '{providerId}' AND r.deleted = false";

                queryTasks.Add(_cosmosRepository.QueryPartitionedEntity<TestScenarioResult>(sql, partitionEntityId: providerId));
            }

            await TaskHelper.WhenAllAndThrow(queryTasks.ToArray());

            List<TestScenarioResult> result = new List<TestScenarioResult>();
            foreach (Task<IEnumerable<TestScenarioResult>> queryTask in queryTasks)
            {
                IEnumerable<TestScenarioResult> providerSourceDatasets = queryTask.Result;
                if (!providerSourceDatasets.IsNullOrEmpty())
                {

                    result.AddRange(providerSourceDatasets);
                }
            }

            return result;
        }

        public async Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> providerResult)
        {
            Guard.ArgumentNotNull(providerResult, nameof(providerResult));

            List<TestScenarioResult> items = new List<TestScenarioResult>(providerResult);
            List<KeyValuePair<string, TestScenarioResult>> resultItems = new List<KeyValuePair<string, TestScenarioResult>>(items.Count());

            for (int i = 0; i < items.Count; i++)
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
    }
}
