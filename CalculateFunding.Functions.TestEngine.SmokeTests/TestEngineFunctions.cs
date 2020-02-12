using CalculateFunding.Common.Models;
using CalculateFunding.Functions.TestEngine.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.TestEngine.SmokeTests
{
    [TestClass]
    public class TestEngineFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static ITestResultsService _testResultsService;
        private static ITestEngineService _testEngineService;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("testengine");

            _logger = CreateLogger();
            _testResultsService = CreateTestResultsService();
            _testEngineService = CreateTestEngineService();
        }

        [TestMethod]
        public async Task OnDeleteTestResults_SmokeTestSucceeds()
        {
            OnDeleteTestResults onDeleteTestResults = new OnDeleteTestResults(_logger,
                _testResultsService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteTestResults,
                (Message smokeResponse) => onDeleteTestResults.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnEditSpecificationEventFired_SmokeTestSucceeds()
        {
            OnEditSpecificationEvent onEditSpecificationEvent = new OnEditSpecificationEvent(_logger,
                _testResultsService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateScenarioResultsForEditSpecification,
                (Message smokeResponse) => onEditSpecificationEvent.Run(smokeResponse),
                ServiceBusConstants.TopicNames.EditSpecification);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnTestExecution_SmokeTestSucceeds()
        {
            OnTestExecution onTestExecution = new OnTestExecution(_logger,
                _testEngineService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.TestEngineExecuteTests,
                (Message smokeResponse) => onTestExecution.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnTestSpecificationProviderResultsCleanup_SmokeTestSucceeds()
        {
            OnTestSpecificationProviderResultsCleanup onTestSpecificationProviderResultsCleanup = new OnTestSpecificationProviderResultsCleanup(_logger,
                _testResultsService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.CleanupTestResultsForSpecificationProviders,
                (Message smokeResponse) => onTestSpecificationProviderResultsCleanup.Run(smokeResponse),
                ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static ITestResultsService CreateTestResultsService()
        {
            return Substitute.For<ITestResultsService>();
        }
        
        private static ITestEngineService CreateTestEngineService()
        {
            return Substitute.For<ITestEngineService>();
        }
    }
}
