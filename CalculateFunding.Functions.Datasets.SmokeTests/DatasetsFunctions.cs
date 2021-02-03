using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Datasets.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
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

namespace CalculateFunding.Functions.Datasets.SmokeTests
{
    [TestClass]
    public class DatasetsFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IDatasetDefinitionNameChangeProcessor _datasetDefinitionChangesProcessor;
        private static IProcessDatasetService _processDatasetService;
        private static IDatasetService _datasetService;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("datasets");

            _logger = CreateLogger();

            _datasetDefinitionChangesProcessor = CreateDatasetDefinitionNameChangeProcessor();
            _processDatasetService = CreateProcessDatasetService();
            _datasetService = CreateDatasetService();
            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnDataDefinitionChanges_SmokeTestSucceeds()
        {
            OnDataDefinitionChanges onDataDefinitionChanges = new OnDataDefinitionChanges(_logger,
                _datasetDefinitionChangesProcessor,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateDataDefinitionName,
                async(Message smokeResponse) => await onDataDefinitionChanges.Run(smokeResponse),
                ServiceBusConstants.TopicNames.DataDefinitionChanges);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnMapFdzDatasetsEventFired_SmokeTestSucceeds()
        {
            OnMapFdzDatasetsEventFired onMapFdzDatasetsEventFired = new OnMapFdzDatasetsEventFired(_logger,
                _processDatasetService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.MapFdzDatasets,
                async(Message smokeResponse) => await onMapFdzDatasetsEventFired.Run(smokeResponse), useSession: true);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnDatasetEventFired_SmokeTestSucceeds()
        {
            OnDatasetEvent onDatasetEvent = new OnDatasetEvent(_logger,
                _processDatasetService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.ProcessDataset,
                async(Message smokeResponse) => await onDatasetEvent.Run(smokeResponse), useSession: true);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnDatasetValidationEventFired_SmokeTestSucceeds()
        {
            OnDatasetValidationEvent onDatasetValidationEvent = new OnDatasetValidationEvent(_logger,
                _datasetService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.ValidateDataset,
                async(Message smokeResponse) => await onDatasetValidationEvent.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnDeleteDatasets_SmokeTestSucceeds()
        {
            OnDeleteDatasets onDeleteDatasets = new OnDeleteDatasets(_logger,
                _datasetService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteDatasets,
                async(Message smokeResponse) => await onDeleteDatasets.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
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

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }

    }
}
