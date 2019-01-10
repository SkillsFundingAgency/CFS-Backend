using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Scenarios.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.Scenarios
{
    public class ScenariosService : IScenariosService, IHealthChecker
    {
        private readonly IScenariosRepository _scenariosRepository;
        private readonly ILogger _logger;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IValidator<CreateNewTestScenarioVersion> _createNewTestScenarioVersionValidator;
        private readonly ISearchRepository<ScenarioIndex> _searchRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly IBuildProjectRepository _buildProjectRepository;
        private readonly IVersionRepository<TestScenarioVersion> _versionRepository;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly IFeatureToggle _featureToggle;
        private readonly ICalcsRepository _calcsRepository;
        private readonly Polly.Policy _jobsApiClientPolicy;
        private readonly Polly.Policy _calcsRepositoryPolicy;

        public ScenariosService(
            ILogger logger,
            IScenariosRepository scenariosRepository,
            ISpecificationsRepository specificationsRepository,
            IValidator<CreateNewTestScenarioVersion> createNewTestScenarioVersionValidator,
            ISearchRepository<ScenarioIndex> searchRepository,
            ICacheProvider cacheProvider,
            IMessengerService messengerService,
            IBuildProjectRepository buildProjectRepository,
            IVersionRepository<TestScenarioVersion> versionRepository,
            IJobsApiClient jobsApiClient,
            IFeatureToggle featureToggle,
            ICalcsRepository calcsRepository,
            IScenariosResiliencePolicies scenariosResiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scenariosRepository, nameof(scenariosRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(createNewTestScenarioVersionValidator, nameof(createNewTestScenarioVersionValidator));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(buildProjectRepository, nameof(buildProjectRepository));
            Guard.ArgumentNotNull(versionRepository, nameof(versionRepository));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(scenariosResiliencePolicies, nameof(scenariosResiliencePolicies));

            _scenariosRepository = scenariosRepository;
            _logger = logger;
            _specificationsRepository = specificationsRepository;
            _createNewTestScenarioVersionValidator = createNewTestScenarioVersionValidator;
            _searchRepository = searchRepository;
            _cacheProvider = cacheProvider;
            _messengerService = messengerService;
            _buildProjectRepository = buildProjectRepository;
            _cacheProvider = cacheProvider;
            _versionRepository = versionRepository;
            _jobsApiClient = jobsApiClient;
            _featureToggle = featureToggle;
            _calcsRepository = calcsRepository;
            _jobsApiClientPolicy = scenariosResiliencePolicies.JobsApiClient;
            _calcsRepositoryPolicy = scenariosResiliencePolicies.CalcsRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth scenariosRepoHealth = await ((IHealthChecker)_scenariosRepository).IsHealthOk();
            var searchRepoHealth = await _searchRepository.IsHealthOk();
            var cacheRepoHealth = await _cacheProvider.IsHealthOk();
            string queueName = ServiceBusConstants.QueueNames.CalculationJobInitialiser;
            var messengerServiceHealth = await _messengerService.IsHealthOk(queueName);

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ScenariosService)
            };
            health.Dependencies.AddRange(scenariosRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = messengerServiceHealth.Ok, DependencyName = _messengerService.GetType().GetFriendlyName(), Message = messengerServiceHealth.Message });

            return health;
        }

        async public Task<IActionResult> SaveVersion(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CreateNewTestScenarioVersion scenarioVersion = JsonConvert.DeserializeObject<CreateNewTestScenarioVersion>(json);

            if (scenarioVersion == null)
            {
                _logger.Error("A null scenario version was provided");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            var validationResult = (await _createNewTestScenarioVersionValidator.ValidateAsync(scenarioVersion)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            TestScenario testScenario = null;

            if (!string.IsNullOrEmpty(scenarioVersion.Id))
            {
                testScenario = await _scenariosRepository.GetTestScenarioById(scenarioVersion.Id);
            }

            bool saveAsVersion = true;

            SpecificationSummary specification = await _specificationsRepository.GetSpecificationSummaryById(scenarioVersion.SpecificationId);

            if (specification == null)
            {
                _logger.Error($"Unable to find a specification for specification id : {scenarioVersion.SpecificationId}");

                return new StatusCodeResult(412);
            }

            Reference user = request.GetUserOrDefault();

            if (testScenario == null)
            {
                string Id = Guid.NewGuid().ToString();

                testScenario = new TestScenario
                {
                    Id = Id,
                    SpecificationId = specification.Id,
                    Name = scenarioVersion.Name,
                    Current = new TestScenarioVersion
                    {
                        Date = DateTimeOffset.Now.ToLocalTime(),
                        TestScenarioId = Id,
                        PublishStatus = PublishStatus.Draft,
                        Version = 1,
                        Author = user,
                        Gherkin = scenarioVersion.Scenario,
                        Description = scenarioVersion.Description,
                        FundingPeriodId = specification.FundingPeriod.Id,
                        FundingStreamIds = specification.FundingStreams.Select(s => s.Id).ToArraySafe(),
                    }
                };
            }
            else
            {
                testScenario.Name = scenarioVersion.Name;

                saveAsVersion = !string.Equals(scenarioVersion.Scenario, testScenario.Current.Gherkin) ||
                    scenarioVersion.Description != testScenario.Current.Description;

                TestScenarioVersion newVersion = testScenario.Current.Clone() as TestScenarioVersion;

                if (saveAsVersion == true)
                {
                    newVersion.Author = user;
                    newVersion.Gherkin = scenarioVersion.Scenario;
                    newVersion.Description = scenarioVersion.Description;
                    newVersion.FundingStreamIds = specification.FundingStreams.Select(s => s.Id).ToArraySafe();
                    newVersion.FundingPeriodId = specification.FundingPeriod.Id;

                    newVersion = await _versionRepository.CreateVersion(newVersion, testScenario.Current);

                    testScenario.Current = newVersion;
                }
            }

            HttpStatusCode statusCode = await _scenariosRepository.SaveTestScenario(testScenario);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save test scenario with status code: {statusCode.ToString()}");

                return new StatusCodeResult((int)statusCode);
            }

            await _versionRepository.SaveVersion(testScenario.Current);

            ScenarioIndex scenarioIndex = CreateScenarioIndexFromScenario(testScenario, specification);

            await _searchRepository.Index(new List<ScenarioIndex> { scenarioIndex });

            await _cacheProvider.RemoveAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{testScenario.SpecificationId}");

            await _cacheProvider.RemoveAsync<GherkinParseResult>($"{CacheKeys.GherkinParseResult}{testScenario.Id}");

            if (_featureToggle.IsJobServiceEnabled())
            {
                IEnumerable<Models.Calcs.CalculationCurrentVersion> calculations = await _calcsRepositoryPolicy.ExecuteAsync(() => _calcsRepository.GetCurrentCalculationsBySpecificationId(specification.Id));

                if (calculations.IsNullOrEmpty())
                {
                    _logger.Information($"No calculations found to test for specification id: '{specification.Id}'");
                }
                else
                {
                    string correlationId = request.GetCorrelationId();

                    try
                    {
                        Trigger trigger = new Trigger
                        {
                            EntityId = testScenario.Id,
                            EntityType = nameof(TestScenario),
                            Message = $"Saving test scenario: '{testScenario.Id}'"
                        };

                        bool generateCalculationAggregations = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(calculations.Select(m => m.SourceCode));

                        Job job = await SendInstructAllocationsToJobService(specification.Id, user, trigger, correlationId, generateCalculationAggregations);

                        _logger.Information($"New job of type '{job.JobDefinitionId}' created with id: '{job.Id}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specification.Id}'");

                        return new InternalServerErrorResult($"An error occurred attempting to execute calculations prior to running tests on specification '{specification.Id}'");
                    }
                }
            }
            else
            {
                await SendGenerateAllocationsMessage(specification.Id, request);
            }

            CurrentTestScenario testScenarioResult = await _scenariosRepository.GetCurrentTestScenarioById(testScenario.Id);

            return new OkObjectResult(testScenarioResult);
        }

        async public Task<IActionResult> GetTestScenariosBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetTestScenariusBySpecificationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<TestScenario> testScenarios = await _scenariosRepository.GetTestScenariosBySpecificationId(specificationId);

            return new OkObjectResult(testScenarios.IsNullOrEmpty() ? Enumerable.Empty<TestScenario>() : testScenarios);
        }

        async public Task<IActionResult> GetTestScenarioById(HttpRequest request)
        {
            request.Query.TryGetValue("scenarioId", out var testId);

            var scenarioId = testId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                _logger.Error("No scenario Id was provided to GetTestScenariosById");

                return new BadRequestObjectResult("Null or empty scenario Id provided");
            }

            TestScenario testScenario = await _scenariosRepository.GetTestScenarioById(scenarioId);

            if (testScenario == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(testScenario);
        }

        async public Task<IActionResult> GetCurrentTestScenarioById(HttpRequest request)
        {
            request.Query.TryGetValue("scenarioId", out var testId);

            var scenarioId = testId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                _logger.Error("No scenario Id was provided to GetCurrentTestScenarioById");

                return new BadRequestObjectResult("Null or empty scenario Id provided");
            }

            CurrentTestScenario testScenario = await _scenariosRepository.GetCurrentTestScenarioById(scenarioId);

            if (testScenario == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(testScenario);
        }

        public async Task UpdateScenarioForSpecification(Message message)
        {
            SpecificationVersionComparisonModel specificationVersionComparison = message.GetPayloadAsInstanceOf<SpecificationVersionComparisonModel>();

            if (specificationVersionComparison == null || specificationVersionComparison.Current == null)
            {
                _logger.Error("A null specificationVersionComparison was provided to UpdateScenarioForSpecification");

                throw new InvalidModelException(nameof(Models.Specs.SpecificationVersionComparisonModel), new[] { "Null or invalid model provided" });
            }

            if (specificationVersionComparison.HasNoChanges && !specificationVersionComparison.HasNameChange)
            {
                _logger.Information("No changes detected");
                return;
            }

            string specificationId = specificationVersionComparison.Id;

            IEnumerable<TestScenario> scenarios = await _scenariosRepository.GetTestScenariosBySpecificationId(specificationId);

            if (scenarios.IsNullOrEmpty())
            {
                _logger.Information($"No scenarios found for specification id: {specificationId}");
                return;
            }

            IEnumerable<string> fundingStreamIds = specificationVersionComparison.Current.FundingStreams?.Select(m => m.Id);

            IList<ScenarioIndex> scenarioIndexes = new List<ScenarioIndex>();

            IList<TestScenarioVersion> scenarioVersions = new List<TestScenarioVersion>();

            foreach (TestScenario scenario in scenarios)
            {
                TestScenarioVersion newVersion = new TestScenarioVersion
                {
                    FundingPeriodId = specificationVersionComparison.Current.FundingPeriod.Id,
                    FundingStreamIds = specificationVersionComparison.Current.FundingStreams.Select(m => m.Id),
                    Author = scenario.Current.Author,
                    Gherkin = scenario.Current.Gherkin,
                    Description = scenario.Current.Description,
                    PublishStatus = scenario.Current.PublishStatus
                };

                newVersion = await _versionRepository.CreateVersion(newVersion, scenario.Current);

                scenario.Current = newVersion;

                scenarioVersions.Add(newVersion);

                ScenarioIndex scenarioIndex = CreateScenarioIndexFromScenario(scenario, new SpecificationSummary
                {
                    Id = specificationVersionComparison.Id,
                    Name = specificationVersionComparison.Current.Name,
                    FundingPeriod = specificationVersionComparison.Current.FundingPeriod,
                    FundingStreams = specificationVersionComparison.Current.FundingStreams
                });

                scenarioIndexes.Add(scenarioIndex);
            }

            await TaskHelper.WhenAllAndThrow(
                _scenariosRepository.SaveTestScenarios(scenarios),
                _versionRepository.SaveVersions(scenarioVersions),
                _searchRepository.Index(scenarioIndexes)
                );
        }

        public async Task UpdateScenarioForCalculation(Message message)
        {
            CalculationVersionComparisonModel comparison = message.GetPayloadAsInstanceOf<CalculationVersionComparisonModel>();

            if (comparison == null || comparison.Current == null || comparison.Previous == null)
            {
                _logger.Error("A null CalculationVersionComparisonModel was provided to UpdateScenarioForCalculation");

                throw new InvalidModelException(nameof(SpecificationVersionComparisonModel), new[] { "Null or invalid model provided" });
            }

            if (string.IsNullOrWhiteSpace(comparison.CalculationId))
            {
                _logger.Warning("Null or invalid calculationId provided to UpdateScenarioForCalculation");
                throw new InvalidModelException(nameof(CalculationVersionComparisonModel), new[] { "Null or invalid calculationId provided" });
            }

            if (string.IsNullOrWhiteSpace(comparison.SpecificationId))
            {
                _logger.Warning("Null or invalid SpecificationId provided to UpdateScenarioForCalculation");
                throw new InvalidModelException(nameof(CalculationVersionComparisonModel), new[] { "Null or invalid SpecificationId provided" });
            }

            int updateCount = await UpdateTestScenarioCalculationGherkin(comparison);
            string calculationId = comparison.CalculationId;

            _logger.Information("A total of {updateCount} Test Scenarios updated for calculation ID '{calculationId}'", updateCount, calculationId);
        }

        public async Task<int> UpdateTestScenarioCalculationGherkin(CalculationVersionComparisonModel comparison)
        {
            Guard.ArgumentNotNull(comparison, nameof(comparison));

            if (comparison.Current.Name == comparison.Previous.Name)
            {
                return 0;
            }

            int updateCount = 0;

            IEnumerable<TestScenario> testScenarios = await _scenariosRepository.GetTestScenariosBySpecificationId(comparison.SpecificationId);
            foreach (TestScenario testScenario in testScenarios)
            {
                string sourceString = $" the result for '{comparison.Previous.Name}'";
                string replacementString = $" the result for '{comparison.Current.Name}'";

                string result = Regex.Replace(testScenario.Current.Gherkin, sourceString, replacementString, RegexOptions.IgnoreCase);
                if (result != testScenario.Current.Gherkin)
                {
                    TestScenarioVersion testScenarioVersion = testScenario.Current.Clone() as TestScenarioVersion;
                    testScenarioVersion.Gherkin = result;

                    testScenarioVersion = await _versionRepository.CreateVersion(testScenarioVersion, testScenario.Current);

                    testScenario.Current = testScenarioVersion;

                    await _scenariosRepository.SaveTestScenario(testScenario);

                    await _versionRepository.SaveVersion(testScenarioVersion);

                    await _cacheProvider.RemoveAsync<GherkinParseResult>($"{CacheKeys.GherkinParseResult}{testScenario.Id}");

                    updateCount++;
                }
            }

            if (updateCount > 0)
            {
                await _cacheProvider.RemoveAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{comparison.SpecificationId}");
            }

            return updateCount;
        }

        IDictionary<string, string> CreateMessageProperties(HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>
            {
                { "sfa-correlationId", request.GetCorrelationId() }
            };

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }

        private async Task SendGenerateAllocationsMessage(string specificationId, HttpRequest request)
        {
            IDictionary<string, string> properties = request.BuildMessageProperties();

            properties.Add("specification-id", specificationId);

            properties.Add("ignore-save-provider-results", "true");

            await _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.CalculationJobInitialiser,
                null,
                properties);
            
        }

        private async Task<Job> SendInstructAllocationsToJobService(string specificationId, Reference user, Trigger trigger, string correlationId, bool generateAggregations = false)
        {
            JobCreateModel job = new JobCreateModel
            {
                InvokerUserDisplayName = user.Name,
                InvokerUserId = user.Id,
                JobDefinitionId = generateAggregations ? JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob : JobConstants.DefinitionNames.CreateInstructAllocationJob,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>
                {
                    { "specification-id", specificationId },
                    { "ignore-save-provider-results", "true" }
                },
                Trigger = trigger,
                CorrelationId = correlationId
            };

            return await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(job));
        }

        ScenarioIndex CreateScenarioIndexFromScenario(TestScenario testScenario, SpecificationSummary specification)
        {
            return new ScenarioIndex
            {
                Id = testScenario.Id,
                Name = testScenario.Name,
                Description = testScenario.Current.Description,
                SpecificationId = testScenario.SpecificationId,
                SpecificationName = specification.Name,
                FundingPeriodId = specification.FundingPeriod.Id,
                FundingPeriodName = specification.FundingPeriod.Name,
                FundingStreamIds = specification.FundingStreams?.Select(s => s.Id).ToArray(),
                FundingStreamNames = specification.FundingStreams?.Select(s => s.Name).ToArray(),
                Status = testScenario.Current.PublishStatus.ToString(),
                LastUpdatedDate = DateTimeOffset.Now
            };
        }
    }
}
