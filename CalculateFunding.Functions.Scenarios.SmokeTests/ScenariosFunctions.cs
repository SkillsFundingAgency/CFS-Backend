using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Scenarios.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Scenarios.Interfaces;
using CalculateFunding.Tests.Common;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Scenarios.SmokeTests
{
    [TestClass]
    public class ScenariosFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IDatasetDefinitionFieldChangesProcessor _datasetDefinitionFieldChangesProcessor;
        private static IScenariosService _scenariosService;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("scenarios");

            _logger = CreateLogger();
            _datasetDefinitionFieldChangesProcessor = CreateDatasetDefinitionFieldChangesProcessor();
            _scenariosService = CreateScenariosService();
            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnDataDefinitionChanges_SmokeTestSucceeds()
        {
            OnDataDefinitionChanges onDataDefinitionChanges = new OnDataDefinitionChanges(_logger,
                _datasetDefinitionFieldChangesProcessor,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                 IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateScenarioFieldDefinitionProperties,
                async(Message smokeResponse) => await onDataDefinitionChanges.Run(smokeResponse),
                ServiceBusConstants.TopicNames.DataDefinitionChanges);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnDeleteTests_SmokeTestSucceeds()
        {
            OnDeleteTests onDeleteTests = new OnDeleteTests(_logger,
                _scenariosService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                 IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteTests,
                async(Message smokeResponse) => await onDeleteTests.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnEditCalculationEventFired_SmokeTestSucceeds()
        {
            OnEditCalculationEvent onEditCalculationEvent = new OnEditCalculationEvent(_logger,
                _scenariosService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                 IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditCalculation,
                async(Message smokeResponse) => await onEditCalculationEvent.Run(smokeResponse),
                ServiceBusConstants.TopicNames.EditCalculation);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnEditSpecificationEventFired_SmokeTestSucceeds()
        {
            OnEditSpecificationEvent onEditSpecificationEvent = new OnEditSpecificationEvent(_logger,
                _scenariosService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                 IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditSpecification,
                async(Message smokeResponse) => await onEditSpecificationEvent.Run(smokeResponse),
                ServiceBusConstants.TopicNames.EditSpecification);

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static IDatasetDefinitionFieldChangesProcessor CreateDatasetDefinitionFieldChangesProcessor()
        {
            return Substitute.For<IDatasetDefinitionFieldChangesProcessor>();
        }
        
        private static IScenariosService CreateScenariosService()
        {
            return Substitute.For<IScenariosService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
