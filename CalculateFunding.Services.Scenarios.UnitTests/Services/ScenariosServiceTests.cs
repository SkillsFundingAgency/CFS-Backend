﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Scenarios.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using Calculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using CalculationVersion = CalculateFunding.Models.Calcs.CalculationVersion;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Scenarios.Services
{
    [TestClass]
    public class ScenariosServiceTests
    {
        const string specificationId = "spec-id";
        const string name = "scenario name";
        const string description = "scenario description";
        const string scenario = "scenario";
        const string scenarioId = "scenario-id";

        [TestMethod]
        async public Task SaveVersion_GivenNullScenarioVersion_ReturnsBadRequestObject()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(logger: logger);

            //Act
            IActionResult result = await service.SaveVersion(null, null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("A null scenario version was provided"));

        }

        [TestMethod]
        async public Task SaveVersion_GivenInvalidModel_ReturnsBadRequestObject()
        {
            //Arrange
            CreateNewTestScenarioVersion model = new CreateNewTestScenarioVersion();
            
            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<CreateNewTestScenarioVersion> validator = CreateValidator(validationResult);

            ScenariosService service = CreateScenariosService(logger: logger, createNewTestScenarioVersionValidator: validator);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

        }

        [TestMethod]
        async public Task SaveVersion_GivenNoScenarioIdAndSpecificationDoesNotExist_ReturnsPreConditionFailed()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();

            ILogger logger = CreateLogger();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, null));


            ScenariosService service = CreateScenariosService(logger: logger, specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a specification for specification id : {specificationId}"));

        }

        [TestMethod]
        async public Task SaveVersion_GivenNoScenarioIdButSavingCausesInternalServerError_ReturnsInternalServerError()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.InternalServerError);

            ScenariosService service = CreateScenariosService(
                logger: logger,
                specificationsApiClient: specificationsApiClient,
                scenariosRepository: scenariosRepository);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save test scenario with status code: InternalServerError"));
        }

        [TestMethod]
        async public Task SaveVersion_GivenNoScenarioIdAndSavesScenario_UpdateSearchReturnsOK()
        {
            // Arrange
            CreateNewTestScenarioVersion model = CreateModel();

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ScenariosService service = CreateScenariosService(logger: logger,
                specificationsApiClient: specificationsApiClient, scenariosRepository: scenariosRepository,
                searchRepository: searchrepository);

            scenariosRepository
                .GetCurrentTestScenarioById(Arg.Any<string>())
                .Returns(new CurrentTestScenario());

            // Act
            IActionResult result = await service.SaveVersion(model, null, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<CurrentTestScenario>();

            await
                 searchrepository
                 .Received(1)
                 .Index(Arg.Is<IList<ScenarioIndex>>(m => !string.IsNullOrWhiteSpace(m.First().Id) &&
                                 m.First().Description == description &&
                                 m.First().Name == name &&
                                 m.First().SpecificationId == specificationId &&
                                 m.First().FundingPeriodId == specification.FundingPeriod.Id &&
                                 m.First().FundingPeriodName == specification.FundingPeriod.Name &&
                                 m.First().FundingStreamIds.First() == specification.FundingStreams.First().Id &&
                                 m.First().FundingStreamNames.First() == specification.FundingStreams.First().Name &&
                                 m.First().Status == "Draft" &&
                                 m.First().LastUpdatedDate.HasValue &&
                                 m.First().LastUpdatedDate.Value.Date == DateTime.Now.Date));

            await scenariosRepository
            .Received(1)
            .GetCurrentTestScenarioById(Arg.Any<string>());

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Any<TestScenario>());
        }

        [TestMethod]
        async public Task SaveVersion_GivenScenarioIdAndSavesScenario_UpdateSearchReturnsOK()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioId;

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioId,
                SpecificationId = specificationId,
                Name = name,
                Current = new TestScenarioVersion()
                {
                    Description = description,
                    FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                    FundingPeriodId = specification.FundingPeriod.Id,
                }
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            TestScenarioVersion testScenarioVersion = testScenario.Current.Clone() as TestScenarioVersion;

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsApiClient: specificationsApiClient,
                versionRepository: versionRepository);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                 searchrepository
                 .Received(1)
                 .Index(Arg.Is<IList<ScenarioIndex>>(m => !string.IsNullOrWhiteSpace(m.First().Id) &&
                                 m.First().Description == description &&
                                 m.First().Name == name &&
                                 m.First().SpecificationId == specificationId &&
                                 m.First().FundingPeriodId == specification.FundingPeriod.Id &&
                                 m.First().FundingPeriodName == specification.FundingPeriod.Name &&
                                 m.First().FundingStreamIds.First() == specification.FundingStreams.First().Id &&
                                 m.First().FundingStreamNames.First() == specification.FundingStreams.First().Name &&
                                 m.First().Status == "Draft" &&
                                 m.First().LastUpdatedDate.HasValue &&
                                 m.First().LastUpdatedDate.Value.Date == DateTime.Now.Date));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(testScenarioVersion));
        }

        [TestMethod]
        async public Task SaveVersion_GivenScenarioAndGherkinIsUnchanged_DoesNotCreateNewVersion()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioId;

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenarioVersion testScenarioVersion = new TestScenarioVersion
            {
                Gherkin = "scenario",
                Description = description,
                FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                FundingPeriodId = specification.FundingPeriod.Id,
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioId,
                SpecificationId = specificationId,
                Name = name,
                Current = testScenarioVersion
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specification.Id))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        async public Task SaveVersion_GivenScenarioAndGherkinIsChanged_ThenCreatesNewVersion()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioId;
            model.Scenario = "updated gherkin";

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenarioVersion testScenarioVersion = new TestScenarioVersion
            {
                Gherkin = "scenario",
                Description = description,
                FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                FundingPeriodId = specification.FundingPeriod.Id,
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioId,
                SpecificationId = specificationId,
                Name = name,
                Current = testScenarioVersion
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specification.Id))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsApiClient: specificationsApiClient,
                versionRepository: versionRepository);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await scenariosRepository
                .Received(1)
                .GetCurrentTestScenarioById(Arg.Is(scenarioId));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(testScenarioVersion));
        }

        [TestMethod]
        async public Task SaveVersion_GivenScenarioAndDescriptionIsChanged_ThenCreatesNewVersion()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioId;
            model.Description = "updated description";

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenarioVersion testScenarioVersion = new TestScenarioVersion
            {
                Gherkin = "scenario",
                Description = description,
                FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                FundingPeriodId = specification.FundingPeriod.Id,
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioId,
                SpecificationId = specificationId,
                Name = name,
                Current = testScenarioVersion
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specification.Id))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsApiClient: specificationsApiClient, versionRepository: versionRepository);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await scenariosRepository
                .Received(1)
                .GetCurrentTestScenarioById(Arg.Is(scenarioId));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(testScenarioVersion));
        }

        [TestMethod]
        async public Task SaveVersion_GivenNoCalculationsFound_DoesNotCreateAllocationsJob()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioId;

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioId,
                SpecificationId = specificationId,
                Name = name,
                Current = new TestScenarioVersion()
                {
                    Description = description,
                    FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                    FundingPeriodId = specification.FundingPeriod.Id,
                }
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            TestScenarioVersion testScenarioVersion = testScenario.Current.Clone() as TestScenarioVersion;

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetCurrentCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<Calculation>)null);

            IJobManagement jobManagement = CreateJobManagement();

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsApiClient: specificationsApiClient,
                versionRepository: versionRepository,
                calcsRepository: calcsRepository,
                jobManagement: jobManagement);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                jobManagement
                .DidNotReceive()
                .QueueJob(Arg.Any<JobCreateModel>());

            logger
                .Received(1)
                .Information($"No calculations found to test for specification id: '{specificationId}'");
        }

        [TestMethod]
        async public Task SaveVersion_GivenCreatingJobThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioId;

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioId,
                SpecificationId = specificationId,
                Name = name,
                Current = new TestScenarioVersion()
                {
                    Description = description,
                    FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                    FundingPeriodId = specification.FundingPeriod.Id,
                }
            };

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                    SourceCode = "return 100"
                }
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            TestScenarioVersion testScenarioVersion = testScenario.Current.Clone() as TestScenarioVersion;

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetCurrentCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .When(x => x.QueueJob(Arg.Any<JobCreateModel>()))
                .Do(x => { throw new Exception(); });

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsApiClient: specificationsApiClient,
                versionRepository: versionRepository,
                calcsRepository: calcsRepository,
                jobManagement: jobManagement);

            //Act
            IActionResult result = await service.SaveVersion(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"An error occurred attempting to execute calculations prior to running tests on specification '{specificationId}'");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'"));
        }

        [TestMethod]
        async public Task SaveVersion_GivenJobCreatedForNonAggregatedCalculation_ReturnsOKResult()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioId;

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioId,
                SpecificationId = specificationId,
                Name = name,
                Current = new TestScenarioVersion()
                {
                    Description = description,
                    FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                    FundingPeriodId = specification.FundingPeriod.Id,
                }
            };

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                    SourceCode = "return 100"
                }
            };

            Job job = new Job
            {
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationJob,
                Id = "job-id-1"
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            TestScenarioVersion testScenarioVersion = testScenario.Current.Clone() as TestScenarioVersion;

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetCurrentCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(job);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsApiClient: specificationsApiClient,
                versionRepository: versionRepository,
                calcsRepository: calcsRepository,
                jobManagement: jobManagement);

            //Act
            IActionResult result = await service.SaveVersion(model, null, "corr-2");

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: '{job.Id}'"));

            await
                jobManagement
                    .Received(1)
                    .QueueJob(Arg.Is<JobCreateModel>(m =>
                        m.SpecificationId == specificationId &&
                        m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                        !string.IsNullOrWhiteSpace(m.CorrelationId) &&
                        m.Properties["specification-id"] == specificationId &&
                        m.Properties.ContainsKey("ignore-save-provider-results") &&
                        m.Trigger.EntityId == scenarioId &&
                        m.Trigger.EntityType == nameof(TestScenario) &&
                        m.Trigger.Message == $"Saving test scenario: '{scenarioId}'"));
        }

        [TestMethod]
        async public Task SaveVersion_GivenJobCreatedForAggregatedCalculation_ReturnsOKResult()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioId;

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioId,
                SpecificationId = specificationId,
                Name = name,
                Current = new TestScenarioVersion()
                {
                    Description = description,
                    FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                    FundingPeriodId = specification.FundingPeriod.Id,
                }
            };

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                    SourceCode = "return Sum(Calc1)"
                }
            };

            Job job = new Job
            {
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob,
                Id = "job-id-1"
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            TestScenarioVersion testScenarioVersion = testScenario.Current.Clone() as TestScenarioVersion;

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetCurrentCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(job);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsApiClient: specificationsApiClient,
                versionRepository: versionRepository,
                calcsRepository: calcsRepository,
                jobManagement: jobManagement);

            //Act
            IActionResult result = await service.SaveVersion(model, null, "corr-1");

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob}' created with id: '{job.Id}'"));

            await
                jobManagement
                    .Received(1)
                    .QueueJob(Arg.Is<JobCreateModel>(m =>
                        m.SpecificationId == specificationId &&
                        m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob &&
                        !string.IsNullOrWhiteSpace(m.CorrelationId) &&
                        m.Properties["specification-id"] == specificationId &&
                        m.Properties.ContainsKey("ignore-save-provider-results") &&
                        m.Trigger.EntityId == scenarioId &&
                        m.Trigger.EntityType == nameof(TestScenario) &&
                        m.Trigger.Message == $"Saving test scenario: '{scenarioId}'"));
        }

        [TestMethod]
        public async Task GetTestScenarioById_GivenNoScenarioId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(logger: logger);

            //Act
            IActionResult result = await service.GetTestScenarioById(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error("No scenario Id was provided to GetTestScenariosById");
        }

        [TestMethod]
        public async Task GetTestScenarioById_GivenScenarioIdButNoScenarioWasFound_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns((TestScenario)null);

            ScenariosService service = CreateScenariosService(logger, scenariosRepository);

            //Act
            IActionResult result = await service.GetTestScenarioById(scenarioId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetTestScenarioById_GivenScenarioIdAndScenarioWasFound_ReturnsOKResult()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioId))
                .Returns(new TestScenario());

            ScenariosService service = CreateScenariosService(logger, scenariosRepository);

            //Act
            IActionResult result = await service.GetTestScenarioById(scenarioId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetCurrentTestScenarioById_GivenNoScenarioId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(logger: logger);

            //Act
            IActionResult result = await service.GetCurrentTestScenarioById(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error("No scenario Id was provided to GetCurrentTestScenarioById");
        }

        [TestMethod]
        public async Task GetCurrentTestScenarioById_GivenScenarioIdButNoScenarioWasFound_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetCurrentTestScenarioById(Arg.Is(scenarioId))
                .Returns((CurrentTestScenario)null);

            ScenariosService service = CreateScenariosService(logger, scenariosRepository);

            //Act
            IActionResult result = await service.GetCurrentTestScenarioById(scenarioId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetCurrentTestScenarioById_GivenScenarioIdAndScenarioWasFound_ReturnsOKResult()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetCurrentTestScenarioById(Arg.Is(scenarioId))
                .Returns(new CurrentTestScenario());

            ScenariosService service = CreateScenariosService(logger, scenariosRepository);

            //Act
            IActionResult result = await service.GetCurrentTestScenarioById(scenarioId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public void UpdateScenarioForSpecification_GivenInvalidModel_LogsDoesNotSave()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ScenariosService service = CreateScenariosService();

            //Act
            Func<Task> test = async () => await service.Process(message);

            //Assert
            test
              .Should()
              .ThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task UpdateScenarioForSpecification_GivenModelHasNoChanges_LogsAndReturns()
        {
            //Arrange
            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel
            {
                Current = new Models.Messages.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } },
                Previous = new Models.Messages.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(logger: logger);

            //Act
            await service.Process(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is("No changes detected"));
        }

        [TestMethod]
        public async Task UpdateScenarioForSpecification_GivenModelHasChangedFundingPeriodsButCalcculationsCouldNotBeFound_LogsAndReturns()
        {
            //Arrange
            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel
            {
                Id = specificationId,
                Current = new Models.Messages.SpecificationVersion { FundingPeriod = new Reference { Id = "fp2" } },
                Previous = new Models.Messages.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IScenariosRepository scenarioRepository = CreateScenariosRepository();
            scenarioRepository
                .GetTestScenariosBySpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<TestScenario>)null);

            ScenariosService service = CreateScenariosService(logger, scenarioRepository);

            //Act
            await service.Process(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No scenarios found for specification id: {specificationId}"));
        }

        [TestMethod]
        public async Task UpdateScenarioForSpecification_GivenModelHasChangedFundingPeriodsButBuildProjectNotFound_SavesToCosmosAndSearch()
        {
            //Arrange
            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Messages.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp2" },
                    Name = "any-name",
                    FundingStreams = new[] { new Reference { Id = "fs1" } }
                },
                Previous = new Models.Messages.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<TestScenario> scenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-id",
                    Name = "scenario",
                    SpecificationId = specificationId,
                    Current = new TestScenarioVersion
                    {
                        Author = new Reference("user-id", "username"),
                        Date = DateTimeOffset.Now,
                        PublishStatus = Models.Versioning.PublishStatus.Draft,
                        Gherkin = "source code",
                        Version = 1
                    }
                }
            };

            IScenariosRepository scenarioRepository = CreateScenariosRepository();
            scenarioRepository
                .GetTestScenariosBySpecificationId(Arg.Is(specificationId))
                .Returns(scenarios);

            TestScenarioVersion testScenarioVersion = scenarios.First().Current.Clone() as TestScenarioVersion;
            testScenarioVersion.Version = 2;

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ISearchRepository<ScenarioIndex> searchRepository = CreateSearchRepository();

            ScenariosService service = CreateScenariosService(logger, scenarioRepository, searchRepository: searchRepository, versionRepository: versionRepository);

            //Act
            await service.Process(message);

            //Assert
            scenarios
                .First()
                .Current
                .Version
                .Should()
                .Be(2);

            await
                scenarioRepository
                .Received(1)
                .SaveTestScenarios(Arg.Is(scenarios));

            await
               searchRepository
               .Received(1)
               .Index(Arg.Is<List<ScenarioIndex>>(
                   m => m.Count() == 1 &&
                        m.First().Id == scenarios.First().Id &&
                        m.First().Name == scenarios.First().Name &&
                        m.First().Description == scenarios.First().Current.Description &&
                        m.First().SpecificationId == scenarios.First().SpecificationId
                   ));

            await
              versionRepository
               .Received(1)
               .SaveVersions(Arg.Is<IEnumerable<TestScenarioVersion>>(m => m.First() == testScenarioVersion));
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenCalculationNameChanges_ThenTestScenariosUpdated()
        {
            // Arrange
            const string initialGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name' is greater than '1'";
            const string initialGherkin2 = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Other' is greater than '1'";

            const string expectedChangedGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Updated' is greater than '1'";

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider, versionRepository: versionRepository);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                PreviousName = "Calc Name",
                CurrentName = "Calc Name Updated",
            };

            List<TestScenario> testScenarios = new List<TestScenario>()
            {
                 new TestScenario()
                 {
                    Current = new TestScenarioVersion()
                    {
                        Gherkin = initialGherkin,
                    },
                    Id = "ts1",
                    SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin2,

                      },
                      Id = "ts2",
                      SpecificationId = comparison.SpecificationId,
                 }
            };

            TestScenarioVersion testScenarioVersion = testScenarios.ElementAt(1).Current.Clone() as TestScenarioVersion;
            testScenarioVersion.Gherkin = expectedChangedGherkin;

            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId))
                .Returns(testScenarios);

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(1);

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Is<TestScenario>(s => s.Current.Gherkin == expectedChangedGherkin));

            await scenariosRepository
                .Received(1)
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<TestScenario>>(Arg.Is($"{CacheKeys.TestScenarios}{comparison.SpecificationId}"));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{testScenarios[0].Id}"));
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenCalculationNameChanges_ThenMultipleTestScenariosUpdated()
        {
            // Arrange
            const string initialGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name' is greater than '1'";
            const string initialGherkin2 = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Other' is greater than '1'";
            const string initialGherkin3 = "Given the dataset 'Test DS' field 'Field Name Two' is greater than '0'\r\nThen the result for 'Calc Name' is greater than '1'";

            const string expectedChangedGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Updated' is greater than '1'";
            const string expectedChangedGherkin2 = "Given the dataset 'Test DS' field 'Field Name Two' is greater than '0'\r\nThen the result for 'Calc Name Updated' is greater than '1'";

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider, versionRepository: versionRepository);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                PreviousName = "Calc Name",
                CurrentName = "Calc Name Updated",
            };

            List<TestScenario> testScenarios = new List<TestScenario>()
            {
                 new TestScenario()
                 {
                    Current = new TestScenarioVersion()
                    {
                        Gherkin = initialGherkin,
                    },
                    Id = "ts1",
                    SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin2,

                      },
                       Id = "ts2",
                        SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin3,

                      },
                      Id = "ts3",
                      SpecificationId = comparison.SpecificationId,
                 },
            };

            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId))
                .Returns(testScenarios);

            TestScenarioVersion testScenarioVersion1 = testScenarios.ElementAt(0).Current.Clone() as TestScenarioVersion;
            testScenarioVersion1.Gherkin = expectedChangedGherkin;

            TestScenarioVersion testScenarioVersion2 = testScenarios.ElementAt(1).Current.Clone() as TestScenarioVersion;
            testScenarioVersion2.Gherkin = expectedChangedGherkin2;

            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion1, testScenarioVersion2);

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(2);

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Is<TestScenario>(s => s.Current.Gherkin == expectedChangedGherkin));

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Is<TestScenario>(s => s.Current.Gherkin == expectedChangedGherkin2));

            await scenariosRepository
                .Received(1)
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<TestScenario>>(Arg.Is($"{CacheKeys.TestScenarios}{comparison.SpecificationId}"));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{testScenarios[0].Id}"));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{testScenarios[2].Id}"));
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenCalculationNameChangesWithDifferentCase_ThenTestScenariosUpdated()
        {
            // Arrange
            const string initialGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'calc name' is greater than '1'";
            const string initialGherkin2 = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Other' is greater than '1'";

            const string expectedChangedGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Updated' is greater than '1'";

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider, versionRepository: versionRepository);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                PreviousName = "Calc Name",
                CurrentName = "Calc Name Updated",
            };

            List<TestScenario> testScenarios = new List<TestScenario>()
            {
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin,

                      },
                       Id = "ts1",
                        SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin2,

                      },
                       Id = "ts2",
                        SpecificationId = comparison.SpecificationId,
                 }
            };

            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId))
                .Returns(testScenarios);

            TestScenarioVersion testScenarioVersion = testScenarios.ElementAt(1).Current.Clone() as TestScenarioVersion;
            testScenarioVersion.Gherkin = expectedChangedGherkin;

            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(1);

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Is<TestScenario>(s => s.Current.Gherkin == expectedChangedGherkin));

            await scenariosRepository
                .Received(1)
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<TestScenario>>(Arg.Is($"{CacheKeys.TestScenarios}{comparison.SpecificationId}"));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{testScenarios[0].Id}"));
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenTestScenariosDoesNotContainChangedCalculationGherkin_ThenNoTestScenariosUpdated()
        {
            // Arrange
            const string initialGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'C1' is greater than '1'";
            const string initialGherkin2 = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'C2' is greater than '1'";

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                PreviousName = "Calc Name",
                CurrentName = "Calc Name Updated"
            };

            List<TestScenario> testScenarios = new List<TestScenario>()
            {
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin,

                      },
                       Id = "ts1",
                        SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin2,

                      },
                       Id = "ts2",
                        SpecificationId = comparison.SpecificationId,
                 }
            };

            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId))
                .Returns(testScenarios);

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(0);

            await scenariosRepository
                .Received(0)
                .SaveTestScenario(Arg.Any<TestScenario>());

            await scenariosRepository
                .Received(1)
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId));

            await
                cacheProvider
                .Received(0)
                .RemoveAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{comparison.SpecificationId}");

            await
                cacheProvider
                .Received(0)
                .RemoveAsync<GherkinParseResult>($"{CacheKeys.GherkinParseResult}{testScenarios[0].Id}");
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenCalculationNameNotChanged_ThenNoTestScenariosUpdated()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                PreviousName = "Calc Name",
                CurrentName = "Calc Name"
            };

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(0);

            await scenariosRepository
                .Received(0)
                .SaveTestScenario(Arg.Any<TestScenario>());

            await scenariosRepository
                .Received(0)
                .GetTestScenariosBySpecificationId(Arg.Any<string>());

            await
                cacheProvider
                .Received(0)
                .RemoveAsync<List<TestScenario>>(Arg.Is($"{CacheKeys.TestScenarios}{comparison.SpecificationId}"));

            await
                cacheProvider
                .Received(0)
                .RemoveAsync<GherkinParseResult>(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ScenariosService_UpdateScenarioForCalculation_WhenCalculationNameNotChanged_ThenNoTestScenariosUpdated()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, logger: logger);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                PreviousName = "Calc Name",
                CurrentName = "Calc Name"
            };

            string json = JsonConvert.SerializeObject(comparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            await service.UpdateScenarioForCalculation(message);

            // Assert
            await scenariosRepository
                .Received(0)
                .SaveTestScenario(Arg.Any<TestScenario>());

            await scenariosRepository
                .Received(0)
                .GetTestScenariosBySpecificationId(Arg.Any<string>());

            logger
                .Received(1)
                .Information("A total of {updateCount} Test Scenarios updated for calculation ID '{calculationId}'", Arg.Is(0), Arg.Is(comparison.CalculationId));
        }

        [TestMethod]
        public void ScenariosService_UpdateScenarioForCalculation_WhenModelIsNull_ThenErrorThrown()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, logger: logger);

            CalculationVersionComparisonModel comparison = null;

            string json = JsonConvert.SerializeObject(comparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            Func<Task> action = async () => await service.UpdateScenarioForCalculation(message);

            // Assert
            action
               .Should()
               .ThrowExactly<InvalidModelException>()
               .Which
               .Message
               .Should()
               .Be("The model for type: SpecificationVersionComparisonModel is invalid with the following errors Null or invalid model provided");

            logger
                .Received(1)
                .Error("A null CalculationVersionComparisonModel was provided to UpdateScenarioForCalculation");
        }

        [TestMethod]
        public void ScenariosService_UpdateScenarioForCalculation_WhenCalculationIdIsNull_ThenErrorThrown()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, logger: logger);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CurrentName = "current",
                SpecificationId = "specId",
                CalculationId = null,
                PreviousName = "previous",
            };

            string json = JsonConvert.SerializeObject(comparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            Func<Task> action = async () => await service.UpdateScenarioForCalculation(message);

            // Assert
            action
               .Should()
               .ThrowExactly<InvalidModelException>()
               .Which
               .Message
               .Should()
               .Be("The model for type: CalculationVersionComparisonModel is invalid with the following errors Null or invalid calculationId provided");

            logger
                .Received(1)
                .Warning("Null or invalid calculationId provided to UpdateScenarioForCalculation");
        }

        [TestMethod]
        public void ScenariosService_UpdateScenarioForCalculation_WhenSpecificationIdIsNull_ThenErrorThrown()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, logger: logger);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CurrentName = "current",
                SpecificationId = null,
                CalculationId = "calcId",
                PreviousName = "previous"
            };

            string json = JsonConvert.SerializeObject(comparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            Func<Task> action = async () => await service.UpdateScenarioForCalculation(message);

            // Assert
            action
               .Should()
               .ThrowExactly<InvalidModelException>()
               .Which
               .Message
               .Should()
               .Be("The model for type: CalculationVersionComparisonModel is invalid with the following errors Null or invalid SpecificationId provided");

            logger
                .Received(1)
                .Warning("Null or invalid SpecificationId provided to UpdateScenarioForCalculation");
        }

        [TestMethod]
        public async Task ResetScenarioForFieldDefinitionChanges_GivenNoScenariosToProcess_LogsAndDoesNotContinue()
        {
            //Arrange
            const string specificationId = "spec-id";

            IEnumerable<TestScenario> testScenarios = Enumerable.Empty<TestScenario>();

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = Enumerable.Empty<DatasetSpecificationRelationshipViewModel>();
            IEnumerable<string> currentFieldDefinitionNames = Enumerable.Empty<string>();

            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(specificationId))
                .Returns(testScenarios);

            ScenariosService scenariosService = CreateScenariosService(logger, scenariosRepository);

            //Act
            await scenariosService.ResetScenarioForFieldDefinitionChanges(relationships, specificationId, currentFieldDefinitionNames);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No scenarios found for specification id '{specificationId}'"));
        }

        [TestMethod]
        public async Task ResetScenarioForFieldDefinitionChanges_GivenNoScenariosRequiredResetting_LogsAndDoesNotContinue()
        {
            //Arrange
            const string specificationId = "spec-id";

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = Enumerable.Empty<DatasetSpecificationRelationshipViewModel>();
            IEnumerable<string> currentFieldDefinitionNames = Enumerable.Empty<string>();

            ILogger logger = CreateLogger();

            IEnumerable<TestScenario> scenarios = new[]
            {
                new TestScenario
                {
                     Current = new TestScenarioVersion
                     {
                         Gherkin = "gherkin"
                     }
                }
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(specificationId))
                .Returns(scenarios);

            ScenariosService scenariosService = CreateScenariosService(logger, scenariosRepository);

            //Act
            await scenariosService.ResetScenarioForFieldDefinitionChanges(relationships, specificationId, currentFieldDefinitionNames);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No test scenarios required resetting for specification id '{specificationId}'"));
        }

        [TestMethod]
        public async Task ResetScenarioForFieldDefinitionChanges_GivenScenarioFoundForResetting_UpdatesSenarioAndSavesVersion()
        {
            //Arrange
            const string specificationId = "spec-id";
            const string scenarioId = "id-1";

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = new[]
            {
                new DatasetSpecificationRelationshipViewModel
                {
                     Name = "Test Name"
                }
            };

            IEnumerable<string> currentFieldDefinitionNames = new[]
            {
                "Test Field"
            };

            ILogger logger = CreateLogger();

            IEnumerable<TestScenario> scenarios = new[]
            {
                new TestScenario
                {
                     Current = new TestScenarioVersion
                     {
                         Gherkin = "dataset 'Test Name' field 'TestField'"
                     },
                     Id = scenarioId
                }
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(specificationId))
                .Returns(scenarios);

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            ScenariosService scenariosService = CreateScenariosService(logger, scenariosRepository, versionRepository: versionRepository, cacheProvider: cacheProvider);

            //Act
            await scenariosService.ResetScenarioForFieldDefinitionChanges(relationships, specificationId, currentFieldDefinitionNames);

            //Assert
            await
                scenariosRepository
                    .Received(1)
                    .SaveTestScenario(Arg.Any<TestScenario>());

            await
               versionRepository
                   .Received(1)
                   .SaveVersion(Arg.Any<TestScenarioVersion>());

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{scenarioId}"));
        }

        static ScenariosService CreateScenariosService(
            ILogger logger = null,
            IScenariosRepository scenariosRepository = null,
            ISpecificationsApiClient specificationsApiClient = null,
            IValidator<CreateNewTestScenarioVersion> createNewTestScenarioVersionValidator = null,
            ISearchRepository<ScenarioIndex> searchRepository = null,
            ICacheProvider cacheProvider = null,          
            IVersionRepository<TestScenarioVersion> versionRepository = null,
            IJobManagement jobManagement = null,
            ICalcsRepository calcsRepository = null,
            IScenariosResiliencePolicies scenariosResiliencePolicies = null)
        {
            return new ScenariosService(
                logger ?? CreateLogger(),
                scenariosRepository ?? CreateScenariosRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                createNewTestScenarioVersionValidator ?? CreateValidator(),
                searchRepository ?? CreateSearchRepository(),
                cacheProvider ?? CreateCacheProvider(),             
                versionRepository ?? CreateVersionRepository(),
                jobManagement ?? CreateJobManagement(),
                calcsRepository ?? CreateCalcsRepository(),
                scenariosResiliencePolicies ?? ScenariosResilienceTestHelper.GenerateTestPolicies());
        }

        static IVersionRepository<TestScenarioVersion> CreateVersionRepository()
        {
            return Substitute.For<IVersionRepository<TestScenarioVersion>>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static IScenariosRepository CreateScenariosRepository()
        {
            return Substitute.For<IScenariosRepository>();
        }       

        static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        static IValidator<CreateNewTestScenarioVersion> CreateValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<CreateNewTestScenarioVersion> validator = Substitute.For<IValidator<CreateNewTestScenarioVersion>>();

            validator
               .ValidateAsync(Arg.Any<CreateNewTestScenarioVersion>())
               .Returns(validationResult);

            return validator;
        }

        static ISearchRepository<ScenarioIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ScenarioIndex>>();
        }

        static ICalcsRepository CreateCalcsRepository()
        {
            return Substitute.For<ICalcsRepository>();
        }

        static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        static CreateNewTestScenarioVersion CreateModel()
        {
            return new CreateNewTestScenarioVersion
            {
                SpecificationId = specificationId,
                Name = name,
                Scenario = scenario,
                Description = description
            };
        }
    }
}
