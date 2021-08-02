using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Datasets.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

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
        public async Task OnCreateSpecificationConverterDatasetsMerge_SmokeTestSucceeds()
        {
            OnCreateSpecificationConverterDatasetsMerge onRunConverterDataMerge = new OnCreateSpecificationConverterDatasetsMerge(_logger,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                Substitute.For<ISpecificationConverterDataMerge>(),
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.RunConverterDatasetMerge,
                async smokeResponse => await onRunConverterDataMerge.Run(smokeResponse),
                useSession: true);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnConverterWizardActivityCsvGeneration_SmokeTestSucceeds()
        {
            OnConverterWizardActivityCsvGeneration onConverterWizardActivityCsvGeneration = new OnConverterWizardActivityCsvGeneration(_logger,
                Substitute.For<IConverterWizardActivityCsvGenerationGeneratorService>(),
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.ConverterWizardActivityCsvGeneration,
                async smokeResponse => await onConverterWizardActivityCsvGeneration.Run(smokeResponse),
                useSession: true);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnRunConverterDataMerge_SmokeTestSucceeds()
        {
            OnRunConverterDataMerge onRunConverterDataMerge = new OnRunConverterDataMerge(_logger,
                Substitute.For<IConverterDataMergeService>(),
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.RunConverterDatasetMerge,
                async smokeResponse => await onRunConverterDataMerge.Run(smokeResponse),
                useSession: true);

            response
                .Should()
                .NotBeNull();
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
                async smokeResponse => await onDataDefinitionChanges.Run(smokeResponse),
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
                async smokeResponse => await onMapFdzDatasetsEventFired.Run(smokeResponse),
                useSession: true);

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
                async smokeResponse => await onDatasetEvent.Run(smokeResponse),
                useSession: true);

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
                async smokeResponse => await onDatasetValidationEvent.Run(smokeResponse), useSession:true);

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
                async smokeResponse => await onDeleteDatasets.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger() => Substitute.For<ILogger>();

        private static IDatasetDefinitionNameChangeProcessor CreateDatasetDefinitionNameChangeProcessor() => Substitute.For<IDatasetDefinitionNameChangeProcessor>();

        private static IProcessDatasetService CreateProcessDatasetService() => Substitute.For<IProcessDatasetService>();

        private static IDatasetService CreateDatasetService() => Substitute.For<IDatasetService>();

        private static IUserProfileProvider CreateUserProfileProvider() => Substitute.For<IUserProfileProvider>();
    }
}