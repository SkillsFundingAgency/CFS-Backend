using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.TestEngine.ServiceBus;
using CalculateFunding.Services.Core.Constants;
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
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("testengine");

            _logger = CreateLogger();
            _testResultsService = CreateTestResultsService();
            _testEngineService = CreateTestEngineService();
            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnDeleteTestResults_SmokeTestSucceeds()
        {
            OnDeleteTestResults onDeleteTestResults = new OnDeleteTestResults(_logger,
                _testResultsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteTestResults,
                async(Message smokeResponse) => await onDeleteTestResults.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnEditSpecificationEventFired_SmokeTestSucceeds()
        {
            OnEditSpecificationEvent onEditSpecificationEvent = new OnEditSpecificationEvent(_logger,
                _testResultsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateScenarioResultsForEditSpecification,
                async(Message smokeResponse) => await onEditSpecificationEvent.Run(smokeResponse),
                ServiceBusConstants.TopicNames.EditSpecification);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnTestExecution_SmokeTestSucceeds()
        {
            OnTestExecution onTestExecution = new OnTestExecution(_logger,
                _testEngineService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.TestEngineExecuteTests,
                async(Message smokeResponse) => await onTestExecution.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnTestSpecificationProviderResultsCleanup_SmokeTestSucceeds()
        {
            OnTestSpecificationProviderResultsCleanup onTestSpecificationProviderResultsCleanup = new OnTestSpecificationProviderResultsCleanup(_logger,
                _testResultsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.CleanupTestResultsForSpecificationProviders,
                async(Message smokeResponse) => await onTestSpecificationProviderResultsCleanup.Run(smokeResponse),
                ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup);

            response
                .Should()
                .NotBeNull();
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

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
