using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Functions.Calcs.SmokeTests
{
    [TestClass]
    public class CalculationsFunctions : SmokeTestBase
    {
        private static IApplyTemplateCalculationsService _applyTemplateCalculationsService;
        private static IBuildProjectsService _buildProjectsService;
        private static IJobService _jobService;
        private static IDatasetDefinitionFieldChangesProcessor _datasetDefinitionFieldChangesProcessor;
        private static ICalculationService _calculationService;
        private static ILogger _logger;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("calcs");

            _logger = CreateLogger();

            _applyTemplateCalculationsService = CreateApplyTemplateCalculationsService();
            _buildProjectsService = CreateBuildProjectsService();
            _jobService = CreateJobService();
            _calculationService = CreateCalculationService();
            _datasetDefinitionFieldChangesProcessor = CreateDatasetDefinitionFieldChangesProcessor();
            _userProfileProvider = CreateUserProfileProvider();
        }
        [TestMethod]
        public async Task OnUpdateCodeContextCache_SmokeTestSucceeds()
        {
            OnUpdateCodeContextCache onApplyTemplateCalculations = new OnUpdateCodeContextCache(_logger,
                Substitute.For<ICodeContextCache>(),
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.ApplyTemplateCalculations,
                async(Message smokeResponse) => await onApplyTemplateCalculations.Run(smokeResponse), useSession: true);

            response
                .Should()
                .NotBeNull();
        }
        

        [TestMethod]
        public async Task OnApplyTemplateCalculations_SmokeTestSucceeds()
        {
            OnApplyTemplateCalculations onApplyTemplateCalculations = new OnApplyTemplateCalculations(_logger,
                _applyTemplateCalculationsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.ApplyTemplateCalculations,
                async(Message smokeResponse) => await onApplyTemplateCalculations.Run(smokeResponse), useSession: true);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task CalcsAddRelationshipToBuildProject_SmokeTestSucceeds()
        {
            CalcsAddRelationshipToBuildProject calcsAddRelationshipToBuildProject = new CalcsAddRelationshipToBuildProject(_logger,
                _buildProjectsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships,
                async(Message smokeResponse) => await calcsAddRelationshipToBuildProject.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnCalcsInstructAllocationResults_SmokeTestSucceeds()
        {
            OnCalcsInstructAllocationResults onCalcsInstructAllocationResults = new OnCalcsInstructAllocationResults(_logger,
                _buildProjectsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.CalculationJobInitialiser, 
                async(Message smokeResponse) => await onCalcsInstructAllocationResults.Run(smokeResponse), useSession: true);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnCalculationAggregationsJobCompleted_SmokeTestSucceeds()
        {
            OnCalculationAggregationsJobCompleted onCalculationAggregationsJobCompleted = new OnCalculationAggregationsJobCompleted(_logger,
                _jobService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.CreateInstructAllocationsJob,
                async(Message smokeResponse) => await onCalculationAggregationsJobCompleted.Run(smokeResponse),
                ServiceBusConstants.TopicNames.JobNotifications);

            response
                .Should()
                .NotBeNull();
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

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateCalculationFieldDefinitionProperties,
                async(Message smokeResponse) => await onDataDefinitionChanges.Run(smokeResponse),
                ServiceBusConstants.TopicNames.DataDefinitionChanges);

            response
                .Should()
                .NotBeNull();
        }
        
        [TestMethod]
        public async Task OnDeleteCalculations_SmokeTestSucceeds()
        {
            OnDeleteCalculations onDeleteCalculations = new OnDeleteCalculations(_logger,
                _calculationService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteCalculations,
                async(Message smokeResponse) => await onDeleteCalculations.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IApplyTemplateCalculationsService CreateApplyTemplateCalculationsService()
        {
            return Substitute.For<IApplyTemplateCalculationsService>();
        }

        private static IBuildProjectsService CreateBuildProjectsService()
        {
            return Substitute.For<IBuildProjectsService>();
        }

        private static IJobService CreateJobService()
        {
            return Substitute.For<IJobService>();
        }

        private static IDatasetDefinitionFieldChangesProcessor CreateDatasetDefinitionFieldChangesProcessor()
        {
            return Substitute.For<IDatasetDefinitionFieldChangesProcessor>();
        }
        
        private static ICalculationService CreateCalculationService()
        {
            return Substitute.For<ICalculationService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
