using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Scenarios.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Scenarios.Interfaces;
using CalculateFunding.Tests.Common;
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

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("scenarios");

            _logger = CreateLogger();
            _datasetDefinitionFieldChangesProcessor = CreateDatasetDefinitionFieldChangesProcessor();
            _scenariosService = CreateScenariosService();
        }

        [TestMethod]
        public async Task OnDataDefinitionChanges_SmokeTestSucceeds()
        {
            OnDataDefinitionChanges onDataDefinitionChanges = new OnDataDefinitionChanges(_logger,
                _datasetDefinitionFieldChangesProcessor,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateScenarioFieldDefinitionProperties,
                (Message smokeResponse) => onDataDefinitionChanges.Run(smokeResponse),
                ServiceBusConstants.TopicNames.DataDefinitionChanges);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnDeleteTests_SmokeTestSucceeds()
        {
            OnDeleteTests onDeleteTests = new OnDeleteTests(_logger,
                _scenariosService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteTests,
                (Message smokeResponse) => onDeleteTests.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnEditCalculationEventFired_SmokeTestSucceeds()
        {
            OnEditCalculationEvent onEditCalculationEvent = new OnEditCalculationEvent(_logger,
                _scenariosService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditCalculation,
                (Message smokeResponse) => onEditCalculationEvent.Run(smokeResponse),
                ServiceBusConstants.TopicNames.EditCalculation);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnEditSpecificationEventFired_SmokeTestSucceeds()
        {
            OnEditSpecificationEvent onEditSpecificationEvent = new OnEditSpecificationEvent(_logger,
                _scenariosService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditSpecification,
                (Message smokeResponse) => onEditSpecificationEvent.Run(smokeResponse),
                ServiceBusConstants.TopicNames.EditSpecification);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
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
    }
}
