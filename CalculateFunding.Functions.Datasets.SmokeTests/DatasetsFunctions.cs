using CalculateFunding.Common.Models;
using CalculateFunding.Functions.Datasets.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Datasets.SmokeTests
{
    [TestClass]
    public class DatasetsFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IDatasetDefinitionNameChangeProcessor _datasetDefinitionChangesProcessor;
        private static IProcessDatasetService _processDatasetService;
        private static IDatasetService _datasetService;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("datasets");

            _logger = CreateLogger();

            _datasetDefinitionChangesProcessor = CreateDatasetDefinitionNameChangeProcessor();
            _processDatasetService = CreateProcessDatasetService();
            _datasetService = CreateDatasetService();
        }

        [TestMethod]
        public async Task OnDataDefinitionChanges_SmokeTestSucceeds()
        {
            OnDataDefinitionChanges onDataDefinitionChanges = new OnDataDefinitionChanges(_logger,
                _datasetDefinitionChangesProcessor,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateDataDefinitionName,
                (Message smokeResponse) => onDataDefinitionChanges.Run(smokeResponse),
                ServiceBusConstants.TopicNames.DataDefinitionChanges);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnDatasetEventFired_SmokeTestSucceeds()
        {
            OnDatasetEvent onDatasetEvent = new OnDatasetEvent(_logger,
                _processDatasetService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.ProcessDataset,
                (Message smokeResponse) => onDatasetEvent.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnDatasetValidationEventFired_SmokeTestSucceeds()
        {
            OnDatasetValidationEvent onDatasetValidationEvent = new OnDatasetValidationEvent(_logger,
                _datasetService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.ValidateDataset,
                (Message smokeResponse) => onDatasetValidationEvent.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnDeleteDatasets_SmokeTestSucceeds()
        {
            OnDeleteDatasets onDeleteDatasets = new OnDeleteDatasets(_logger,
                _datasetService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteDatasets,
                (Message smokeResponse) => onDeleteDatasets.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static IDatasetDefinitionNameChangeProcessor CreateDatasetDefinitionNameChangeProcessor()
        {
            return Substitute.For<IDatasetDefinitionNameChangeProcessor>();
        }
        
        private static IProcessDatasetService CreateProcessDatasetService()
        {
            return Substitute.For<IProcessDatasetService>();
        }

        private static IDatasetService CreateDatasetService()
        {
            return Substitute.For<IDatasetService>();
        }

        
    }
}
