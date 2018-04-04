﻿using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.EventHubs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestEngineService : ITestEngineService
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpecificationRepository _specificationRepository;
        private readonly ILogger _logger;
        private readonly ITestEngine _testEngine;
        private readonly IScenariosRepository _scenariosRepository;
        private readonly IProviderRepository _providerRepository;
        private readonly ITestResultsRepository _testResultsRepository;
        private readonly IBuildProjectRepository _buildProjectRepository;

        public TestEngineService(
            ICacheProvider cacheProvider, 
            ISpecificationRepository specificationRepository, 
            ILogger logger,
            ITestEngine testEngine,
            IScenariosRepository scenariosRepository,
            IProviderRepository providerRepository,
            ITestResultsRepository testResultsRepository,
            IBuildProjectRepository buildProjectRepository)
        {
            _cacheProvider = cacheProvider;
            _specificationRepository = specificationRepository;
            _logger = logger;
            _testEngine = testEngine;
            _scenariosRepository = scenariosRepository;
            _providerRepository = providerRepository;
            _testResultsRepository = testResultsRepository;
            _buildProjectRepository = buildProjectRepository;
        }

        public async Task RunTests(EventData message)
        {
            string cacheKey = message.GetPayloadAsInstanceOf<string>();

            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                _logger.Error("Null or empty cache key provided");
                return;
            }

            string specificationId = message.Properties["specificationId"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("Null or empty specification id provided");
                return;
            }

            IEnumerable<ProviderResult> providerResults = await _cacheProvider.GetAsync<List<ProviderResult>>(cacheKey);

            if (providerResults.IsNullOrEmpty())
            {
                _logger.Error($"No provider results found in cache for key: {cacheKey}");
                return;
            }

            await _cacheProvider.RemoveAsync<List<ProviderResult>>(cacheKey);

            IEnumerable<TestScenario> testScenarios = await _scenariosRepository.GetTestScenariosBySpecificationId(specificationId);

            if (testScenarios.IsNullOrEmpty())
            {
                _logger.Warning($"No test scenarios found for specification id: {specificationId}");
                return;
            }

            Specification specification = await _specificationRepository.GetSpecificationById(specificationId);

            if(specification == null)
            {
                _logger.Error($"No specification found for specification id: {specificationId}");
                return;
            }

            IEnumerable<ProviderSourceDataset> sourceDatasets = await _providerRepository.GetProviderSourceDatasetsBySpecificationId(specificationId);

            if (sourceDatasets.IsNullOrEmpty())
            {
                _logger.Error($"No source datasets found for specification id: {specificationId}");
                return;
            }

            BuildProject buildProject = await _buildProjectRepository.GetBuildProjectBySpecificationId(specificationId);

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id).ToList();

            IEnumerable<TestScenarioResult> testScenarioResults = await _testResultsRepository.GetCurrentTestResults(providerIds, specificationId);

            IEnumerable<TestScenarioResult> results = await _testEngine.RunTests(testScenarios, providerResults, sourceDatasets, testScenarioResults, specification, buildProject);

            if (results.Any())
            {
                HttpStatusCode status = await _testResultsRepository.SaveTestProviderResults(results);

                if (!status.IsSuccess())
                {
                    _logger.Error($"Failed to save test results with status code: {status.ToString()}");
                }
            }
        }
    }
}
