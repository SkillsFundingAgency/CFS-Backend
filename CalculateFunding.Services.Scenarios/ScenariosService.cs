using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Scenarios.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios
{
    public class ScenariosService : IScenariosService
    {
        private readonly IScenariosRepository _scenariosRepository;
        private readonly ILogger _logger;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IValidator<CreateNewTestScenarioVersion> _createNewTestScenarioVersionValidator;
        private readonly ISearchRepository<ScenarioIndex> _searchRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly IBuildProjectRepository _buildProjectRepository;

        public ScenariosService(
            ILogger logger, 
            IScenariosRepository scenariosRepository, 
            ISpecificationsRepository specificationsRepository, 
            IValidator<CreateNewTestScenarioVersion> createNewTestScenarioVersionValidator,
            ISearchRepository<ScenarioIndex> searchRepository,
            ICacheProvider cacheProvider,
            IMessengerService messengerService,
            IBuildProjectRepository buildProjectRepository)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scenariosRepository, nameof(scenariosRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(createNewTestScenarioVersionValidator, nameof(createNewTestScenarioVersionValidator));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));

            _scenariosRepository = scenariosRepository;
            _logger = logger;
            _specificationsRepository = specificationsRepository;
            _createNewTestScenarioVersionValidator = createNewTestScenarioVersionValidator;
            _searchRepository = searchRepository;
            _cacheProvider = cacheProvider;
            _messengerService = messengerService;
            _buildProjectRepository = buildProjectRepository;
            _cacheProvider = cacheProvider;
        }

        async public Task<IActionResult> SaveVersion(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CreateNewTestScenarioVersion scenarioVersion = JsonConvert.DeserializeObject<CreateNewTestScenarioVersion>(json);

            if(scenarioVersion == null)
            {
                _logger.Error("A null scenario version was provided");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            var validationResult = (await _createNewTestScenarioVersionValidator.ValidateAsync(scenarioVersion)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            TestScenario testScenario = null;

            if (!string.IsNullOrEmpty(scenarioVersion.Id))
                testScenario = await _scenariosRepository.GetTestScenarioById(scenarioVersion.Id);

            if (testScenario == null) {

                Specification specification = await _specificationsRepository.GetSpecificationById(scenarioVersion.SpecificationId);

                if(specification == null)
                {
                    _logger.Error($"Unable to find a specification for specification id : {scenarioVersion.SpecificationId}");

                    return new StatusCodeResult(412);
                }

                testScenario = new TestScenario
                {
                    Specification = new SpecificationSummary
                    {
                        FundingStream = specification.FundingStream,
                        Id = specification.Id,
                        Name = specification.Name,
                        Period = specification.AcademicYear
                    },
                    Id = Guid.NewGuid().ToString(),
                    Name = scenarioVersion.Name,
                    Description = scenarioVersion.Description,
                    FundingStream = specification.FundingStream,
                    Period = specification.AcademicYear,
                    History = new List<TestScenarioVersion>(),
                    Current = new TestScenarioVersion()
                };
            }

            Reference user = request.GetUser();

            TestScenarioVersion newVersion = new TestScenarioVersion
            {
                Version = GetNextVersionNumberFromCalculationVersions(testScenario.History),
                Author = user,
                Date = DateTime.UtcNow,
                PublishStatus = (testScenario.Current.PublishStatus == PublishStatus.Published
                                    || testScenario.Current.PublishStatus == PublishStatus.Updated)
                                    ? PublishStatus.Updated : PublishStatus.Draft,
                Gherkin = scenarioVersion.Scenario
            };

            testScenario.Current = newVersion;

            testScenario.History.Add(newVersion);

            HttpStatusCode statusCode = await _scenariosRepository.SaveTestScenario(testScenario);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save test scenario with status code: {statusCode.ToString()}");

                return new StatusCodeResult((int)statusCode);
            }

            ScenarioIndex scenarioIndex = new ScenarioIndex
            {
                Id = testScenario.Id,
                Name = testScenario.Name,
                Description = testScenario.Description,
                SpecificationId = testScenario.Specification.Id,
                SpecificationName = testScenario.Specification.Name,
                PeriodId = testScenario.Specification.Period.Id,
                PeriodName = testScenario.Specification.Period.Name,
                FundingStreamId = testScenario.Specification.FundingStream.Id,
                FundingStreamName = testScenario.Specification.FundingStream.Name,
                Status = testScenario.Current.PublishStatus.ToString(),
                LastUpdatedDate = DateTimeOffset.Now
            };

            await _searchRepository.Index(new List<ScenarioIndex> { scenarioIndex });

            await _cacheProvider.RemoveAsync<List<TestScenario>>(testScenario.Specification.Id);

            await _cacheProvider.RemoveAsync<GherkinParseResult>($"gherkin-parse-result:{testScenario.Id}");

            BuildProject buildProject = await _buildProjectRepository.GetBuildProjectBySpecificationId(testScenario.Specification.Id);

            if(buildProject == null)
            {
                _logger.Error($"Failed to find a build project for specification id {testScenario.Specification.Id}");
            }
            else
            {
                await SendGenerateAllocationsMessage(buildProject, request);
            }

            return new OkObjectResult(newVersion);
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

        int GetNextVersionNumberFromCalculationVersions(IEnumerable<TestScenarioVersion> versions)
        {
            if (!versions.Any())
                return 1;

            int maxVersion = versions.Max(m => m.Version);

            return maxVersion + 1;
        }

        IDictionary<string, string> CreateMessageProperties(HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("sfa-correlationId", request.GetCorrelationId());

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }

        Task SendGenerateAllocationsMessage(BuildProject buildProject, HttpRequest request)
        {
            IDictionary<string, string> properties = CreateMessageProperties(request);

            properties.Add("specification-id", buildProject.Specification.Id);

            properties.Add("ignore-save-provider-results", "true");

            return _messengerService.SendToQueue(ServiceBusConstants.QueueNames.CalculationJobInitialiser,
                buildProject,
                properties);
        }
    }
}
