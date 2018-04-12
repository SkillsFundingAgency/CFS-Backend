using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.UnitTests
{
    [TestClass]
    public class TestResultsServiceTests
    {
        [TestMethod]
        public async Task SaveTestProviderResults_WhenNoItemsPasssed_ThenNothingSaved()
        {
            // Arrange
            ITestResultsRepository testResultsRepository = CreateTestResultsRepository();
            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRespository();

            ITestResultsService service = CreateTestResultsService(testResultsRepository, searchRepository);

            IEnumerable<TestScenarioResult> updateItems = Enumerable.Empty<TestScenarioResult>();

            // Act
            HttpStatusCode result = await service.SaveTestProviderResults(updateItems);

            // Assert
            result.Should().Be(HttpStatusCode.NotModified);

            await testResultsRepository
                .Received(0)
                .SaveTestProviderResults(Arg.Any<IEnumerable<TestScenarioResult>>());

            await searchRepository
                .Received(0)
                .Index(Arg.Any<IEnumerable<TestScenarioResultIndex>>());
        }

        [TestMethod]
        public async Task SaveTestProviderResults_WhenItemsPasssed_ThenItemsSaved()
        {
            // Arrange
            ITestResultsRepository testResultsRepository = CreateTestResultsRepository();
            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRespository();

            ITestResultsService service = CreateTestResultsService(testResultsRepository, searchRepository);

            List<TestScenarioResult> itemsToUpdate = new List<TestScenarioResult>();
            itemsToUpdate.Add(CreateTestScenarioResult());

            testResultsRepository
                .SaveTestProviderResults(Arg.Any<IEnumerable<TestScenarioResult>>())
                .Returns(HttpStatusCode.Created);

            // Act
            HttpStatusCode result = await service.SaveTestProviderResults(itemsToUpdate);

            // Assert
            result.Should().Be(HttpStatusCode.Created);

            await testResultsRepository
                .Received(1)
                .SaveTestProviderResults(itemsToUpdate);

            await searchRepository
                .Received(1)
                .Index(Arg.Any<IEnumerable<TestScenarioResultIndex>>());
        }

        [TestMethod]
        public async Task SaveTestProviderResults_WhenItemsPasssed_ThenSearchResultsMapped()
        {
            // Arrange
            ITestResultsRepository testResultsRepository = CreateTestResultsRepository();
            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRespository();

            ITestResultsService service = CreateTestResultsService(testResultsRepository, searchRepository);

            List<TestScenarioResult> itemsToUpdate = new List<TestScenarioResult>();
            TestScenarioResult testScenarioResult = CreateTestScenarioResult();
            itemsToUpdate.Add(testScenarioResult);

            testResultsRepository
                .SaveTestProviderResults(Arg.Any<IEnumerable<TestScenarioResult>>())
                .Returns(HttpStatusCode.Created);

            // Act
            HttpStatusCode result = await service.SaveTestProviderResults(itemsToUpdate);

            // Assert
            result.Should().Be(HttpStatusCode.Created);

            await testResultsRepository
                .Received(1)
                .SaveTestProviderResults(itemsToUpdate);

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<TestScenarioResultIndex>>(c =>
                    c.First().Id == testScenarioResult.Id &&
                    c.First().ProviderId == testScenarioResult.Provider.Id &&
                    c.First().ProviderName == testScenarioResult.Provider.Name &&
                    c.First().SpecificationId == testScenarioResult.Specification.Id &&
                    c.First().SpecificationName == testScenarioResult.Specification.Name &&
                    c.First().TestResult == Enum.GetName(typeof(Models.Results.TestResult), testScenarioResult.TestResult) &&
                    c.First().TestScenarioId == testScenarioResult.TestScenario.Id &&
                    c.First().TestScenarioName == testScenarioResult.TestScenario.Name &&
                    c.First().LastUpdatedDate > DateTime.UtcNow.AddDays(-1)
                ));
        }

        [TestMethod]
        public async Task SaveTestProviderResults_WhenItemsPasssed_ThenTelemetryLogged()
        {
            // Arrange
            ITestResultsRepository testResultsRepository = CreateTestResultsRepository();
            ISearchRepository<TestScenarioResultIndex> searchRepository = CreateSearchRespository();
            ITelemetry telemetry = CreateTelemetry();

            ITestResultsService service = CreateTestResultsService(testResultsRepository, searchRepository, telemetry: telemetry);

            List<TestScenarioResult> itemsToUpdate = new List<TestScenarioResult>();
            TestScenarioResult testScenarioResult = CreateTestScenarioResult();
            itemsToUpdate.Add(testScenarioResult);

            testResultsRepository
                .SaveTestProviderResults(Arg.Any<IEnumerable<TestScenarioResult>>())
                .Returns(HttpStatusCode.Created);

            // Act
            HttpStatusCode result = await service.SaveTestProviderResults(itemsToUpdate);

            // Assert
            result.Should().Be(HttpStatusCode.Created);

            telemetry
                .Received(1)
                .TrackEvent(
                Arg.Is("UpdateTestScenario"),
                Arg.Is<IDictionary<string, string>>(p => 
                    p.ContainsKey("SpecificationId") &&
                    p["SpecificationId"] == testScenarioResult.Specification.Id
                ),
                Arg.Is<IDictionary<string, double>>(
                    m=>m.ContainsKey("update-testscenario-elapsedMilliseconds") &&
                    m.ContainsKey("update-testscenario-recordsUpdated") &&
                    m["update-testscenario-elapsedMilliseconds"] > 0 &&
                    m["update-testscenario-recordsUpdated"] == 1
                    )
                );
        }

        [TestMethod]
        public async Task SaveTestProviderResults_WhenItemsPasssedAndRepositoryFailed_ThenItemsNotSaved()
        {
            // Arrange
            ITestResultsRepository testResultsRepository = CreateTestResultsRepository();

            testResultsRepository
                .SaveTestProviderResults(Arg.Any<IEnumerable<TestScenarioResult>>())
                .Returns(HttpStatusCode.BadRequest);

            ITestResultsService service = CreateTestResultsService(testResultsRepository);

            List<TestScenarioResult> itemsToUpdate = new List<TestScenarioResult>();
            itemsToUpdate.Add(CreateTestScenarioResult());

            // Act
            HttpStatusCode result = await service.SaveTestProviderResults(itemsToUpdate);

            // Assert
            result.Should().Be(HttpStatusCode.InternalServerError);

            await testResultsRepository
                .Received(1)
                .SaveTestProviderResults(itemsToUpdate);
        }

        private TestResultsService CreateTestResultsService(
            ITestResultsRepository testResultsRepository = null,
            ISearchRepository<TestScenarioResultIndex> searchRepository = null,
            IMapper mapper = null,
            ILogger logger = null,
            ITelemetry telemetry = null,
            ResiliencePolicies policies = null)
        {
            return new TestResultsService(
                testResultsRepository ?? CreateTestResultsRepository(),
                searchRepository ?? CreateSearchRespository(),
                mapper ?? CreateMapper(),
                logger ?? CreateLogger(),
                telemetry ?? CreateTelemetry(),
                policies ?? TestRunnerResilienceTestHelper.GenerateTestPolicies()
                );
        }

        private ITestResultsRepository CreateTestResultsRepository()
        {
            return Substitute.For<ITestResultsRepository>();
        }

        private ISearchRepository<TestScenarioResultIndex> CreateSearchRespository()
        {
            return Substitute.For<ISearchRepository<TestScenarioResultIndex>>();
        }

        private IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<ResultsMappingProfile>();
            });

            return new Mapper(config);
        }

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        private TestScenarioResult CreateTestScenarioResult()
        {
            return new TestScenarioResult()
            {
                Provider = new Reference("ProviderId", "Provider Name"),
                Specification = new Reference("SpecificationId", "Specification Name"),
                TestResult = Models.Results.TestResult.Passed,
                TestScenario = new Reference("TestScenarioId", "Test Scenario Name")
            };
        }
    }
}
