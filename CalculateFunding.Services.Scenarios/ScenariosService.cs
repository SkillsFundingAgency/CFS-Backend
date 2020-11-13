using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Scenarios.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using JobsModels = CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Processing;

namespace CalculateFunding.Services.Scenarios
{
    public class ScenariosService : ProcessingService, IScenariosService, IHealthChecker
    {
        private readonly IScenariosRepository _scenariosRepository;
        private readonly ILogger _logger;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IValidator<CreateNewTestScenarioVersion> _createNewTestScenarioVersionValidator;
        private readonly ISearchRepository<ScenarioIndex> _searchRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IVersionRepository<TestScenarioVersion> _versionRepository;
        private readonly IJobManagement _jobManagement;
        private readonly ICalcsRepository _calcsRepository;
        private readonly Polly.AsyncPolicy _calcsRepositoryPolicy;
        private readonly Polly.AsyncPolicy _scenariosRepositoryPolicy;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;

        public ScenariosService(
            ILogger logger,
            IScenariosRepository scenariosRepository,
            ISpecificationsApiClient specificationsApiClient,
            IValidator<CreateNewTestScenarioVersion> createNewTestScenarioVersionValidator,
            ISearchRepository<ScenarioIndex> searchRepository,
            ICacheProvider cacheProvider,         
            IVersionRepository<TestScenarioVersion> versionRepository,
            IJobManagement jobManagement,
            ICalcsRepository calcsRepository,
            IScenariosResiliencePolicies scenariosResiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scenariosRepository, nameof(scenariosRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(createNewTestScenarioVersionValidator, nameof(createNewTestScenarioVersionValidator));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));        
            Guard.ArgumentNotNull(versionRepository, nameof(versionRepository));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(scenariosResiliencePolicies?.CalcsRepository, nameof(scenariosResiliencePolicies.CalcsRepository));
            Guard.ArgumentNotNull(scenariosResiliencePolicies?.ScenariosRepository, nameof(scenariosResiliencePolicies.ScenariosRepository));
            Guard.ArgumentNotNull(scenariosResiliencePolicies?.SpecificationsApiClient, nameof(scenariosResiliencePolicies.SpecificationsApiClient));

            _scenariosRepository = scenariosRepository;
            _logger = logger;
            _specificationsApiClient = specificationsApiClient;
            _createNewTestScenarioVersionValidator = createNewTestScenarioVersionValidator;
            _searchRepository = searchRepository;
            _cacheProvider = cacheProvider;          
            _cacheProvider = cacheProvider;
            _versionRepository = versionRepository;
            _jobManagement = jobManagement;
            _calcsRepository = calcsRepository;
            _calcsRepositoryPolicy = scenariosResiliencePolicies.CalcsRepository;
            _scenariosRepositoryPolicy = scenariosResiliencePolicies.ScenariosRepository;
            _specificationsApiClientPolicy = scenariosResiliencePolicies.SpecificationsApiClient;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth scenariosRepoHealth = await ((IHealthChecker)_scenariosRepository).IsHealthOk();
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ScenariosService)
            };
            health.Dependencies.AddRange(scenariosRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> SaveVersion(CreateNewTestScenarioVersion scenarioVersion, Reference user, string correlationId)
        {
            if (scenarioVersion == null)
            {
                _logger.Error("A null scenario version was provided");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            BadRequestObjectResult validationResult = (await _createNewTestScenarioVersionValidator.ValidateAsync(scenarioVersion)).PopulateModelState();

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

            Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
        await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(scenarioVersion.SpecificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                _logger.Error($"Unable to find a specification for specification id : {scenarioVersion.SpecificationId}");
                return new StatusCodeResult(412);
            }

            SpecModel.SpecificationSummary specification = specificationApiResponse.Content;

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
                _logger.Error($"Failed to save test scenario with status code: {statusCode}");

                return new StatusCodeResult((int)statusCode);
            }

            await _versionRepository.SaveVersion(testScenario.Current);

            ScenarioIndex scenarioIndex = CreateScenarioIndexFromScenario(testScenario, specification);

            await _searchRepository.Index(new List<ScenarioIndex> { scenarioIndex });

            await _cacheProvider.RemoveAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{testScenario.SpecificationId}");

            await _cacheProvider.RemoveAsync<GherkinParseResult>($"{CacheKeys.GherkinParseResult}{testScenario.Id}");

            IEnumerable<Common.ApiClient.Calcs.Models.Calculation> calculations = await _calcsRepositoryPolicy.ExecuteAsync(() => _calcsRepository.GetCurrentCalculationsBySpecificationId(specification.Id));

            if (calculations.IsNullOrEmpty())
            {
                _logger.Information($"No calculations found to test for specification id: '{specification.Id}'");
            }
            else
            {
                try
                {
                    JobsModels.Trigger trigger = new JobsModels.Trigger
                    {
                        EntityId = testScenario.Id,
                        EntityType = nameof(TestScenario),
                        Message = $"Saving test scenario: '{testScenario.Id}'"
                    };

                    bool generateCalculationAggregations = SourceCodeHelpers.HasCalculationAggregateFunctionParameters(calculations.Select(m => m.SourceCode));

                    JobsModels.Job job = await SendInstructAllocationsToJobService(specification.Id, user, trigger, correlationId, generateCalculationAggregations);

                    _logger.Information($"New job of type '{job.JobDefinitionId}' created with id: '{job.Id}'");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specification.Id}'");

                    return new InternalServerErrorResult($"An error occurred attempting to execute calculations prior to running tests on specification '{specification.Id}'");
                }
            }

            CurrentTestScenario testScenarioResult = await _scenariosRepository.GetCurrentTestScenarioById(testScenario.Id);

            return new OkObjectResult(testScenarioResult);
        }

        public async Task<IActionResult> GetTestScenariosBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetTestScenariusBySpecificationId");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<TestScenario> testScenarios = await _scenariosRepository.GetTestScenariosBySpecificationId(specificationId);

            return new OkObjectResult(testScenarios.IsNullOrEmpty() ? Enumerable.Empty<TestScenario>() : testScenarios);
        }

        public async Task<IActionResult> GetTestScenarioById(string scenarioId)
        {
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

        public async Task<IActionResult> GetCurrentTestScenarioById(string scenarioId)
        {
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

        public override async Task Process(Message message)
        {
            SpecificationVersionComparisonModel specificationVersionComparison = message.GetPayloadAsInstanceOf<SpecificationVersionComparisonModel>();

            if (specificationVersionComparison == null || specificationVersionComparison.Current == null)
            {
                _logger.Error("A null specificationVersionComparison was provided to UpdateScenarioForSpecification");

                throw new InvalidModelException(nameof(SpecificationVersionComparisonModel), new[] { "Null or invalid model provided" });
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

                ScenarioIndex scenarioIndex = CreateScenarioIndexFromScenario(scenario, new SpecModel.SpecificationSummary
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
                    await SaveVersion(testScenario, result);

                    updateCount++;
                }
            }

            if (updateCount > 0)
            {
                await _cacheProvider.RemoveAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{comparison.SpecificationId}");
            }

            return updateCount;
        }

        public async Task ResetScenarioForFieldDefinitionChanges(IEnumerable<DatasetSpecificationRelationshipViewModel> relationships, string specificationId, IEnumerable<string> currentFieldDefinitionNames)
        {
            Guard.ArgumentNotNull(relationships, nameof(relationships));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(currentFieldDefinitionNames, nameof(currentFieldDefinitionNames));

            IEnumerable<TestScenario> scenarios = await _scenariosRepositoryPolicy.ExecuteAsync(() => _scenariosRepository.GetTestScenariosBySpecificationId(specificationId));

            if (scenarios.IsNullOrEmpty())
            {
                _logger.Information($"No scenarios found for specification id '{specificationId}'");
                return;
            }

            List<string> fieldIdentifiers = new List<string>();

            foreach (DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel in relationships)
            {
                fieldIdentifiers.AddRange(currentFieldDefinitionNames.Select(m => $"dataset {datasetSpecificationRelationshipViewModel.Name} field {VisualBasicTypeGenerator.GenerateIdentifier(m)}"));
            }

            IEnumerable<TestScenario> scenariosToUpdate = scenarios.Where(m => SourceCodeHelpers.CodeContainsFullyQualifiedDatasetFieldIdentifier(m.Current.Gherkin.RemoveAllQuotes(), fieldIdentifiers));

            if (scenariosToUpdate.IsNullOrEmpty())
            {
                _logger.Information($"No test scenarios required resetting for specification id '{specificationId}'");
                return;
            }

            const string reasonForCommenting = "The dataset definition referenced by this scenario/spec has been updated and subsequently the code has been commented out";

            foreach (TestScenario scenario in scenariosToUpdate)
            {
                string gherkin = scenario.Current.Gherkin;

                string updatedGherkin = SourceCodeHelpers.CommentOutCode(gherkin, reasonForCommenting, commentSymbol: "#");

                await SaveVersion(scenario, updatedGherkin);
            }
        }

        public async Task DeleteTests(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.UserProperties["specification-id"].ToString();
            if (string.IsNullOrEmpty(specificationId))
            {
                string error = "Null or empty specification Id provided for deleting test results";
                _logger.Error(error);
                throw new Exception(error);
            }

            string deletionTypeProperty = message.UserProperties["deletion-type"].ToString();
            if (string.IsNullOrEmpty(deletionTypeProperty))
            {
                string error = "Null or empty deletion type provided for deleting test results";
                _logger.Error(error);
                throw new Exception(error);
            }

            await _scenariosRepository.DeleteTestsBySpecificationId(specificationId, deletionTypeProperty.ToDeletionType());
        }

        private async Task TrackJobFailed(string jobId, Exception exception)
        {
            await AddJobTracking(jobId, new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = exception.ToString()
            });
        }

        private async Task AddJobTracking(string jobId, JobLogUpdateModel tracking)
        {
            await _jobManagement.AddJobLog(jobId, tracking);
        }

        private async Task SaveVersion(TestScenario testScenario, string gherkin)
        {
            TestScenarioVersion testScenarioVersion = testScenario.Current.Clone() as TestScenarioVersion;
            testScenarioVersion.Gherkin = gherkin;

            testScenarioVersion = await _versionRepository.CreateVersion(testScenarioVersion, testScenario.Current);

            testScenario.Current = testScenarioVersion;

            await _scenariosRepositoryPolicy.ExecuteAsync(() => _scenariosRepository.SaveTestScenario(testScenario));

            await _versionRepository.SaveVersion(testScenarioVersion);

            await _cacheProvider.RemoveAsync<GherkinParseResult>($"{CacheKeys.GherkinParseResult}{testScenario.Id}");
        }

        private async Task<JobsModels.Job> SendInstructAllocationsToJobService(string specificationId, Reference user, JobsModels.Trigger trigger, string correlationId, bool generateAggregations = false)
        {
            JobsModels.JobCreateModel job = new JobsModels.JobCreateModel
            {
                InvokerUserDisplayName = user?.Name,
                InvokerUserId = user?.Id,
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

            return await _jobManagement.QueueJob(job);
        }

        private ScenarioIndex CreateScenarioIndexFromScenario(TestScenario testScenario, SpecModel.SpecificationSummary specification)
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
