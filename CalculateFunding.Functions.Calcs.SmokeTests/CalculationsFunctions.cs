using CalculateFunding.Common.Models;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        }

        [TestMethod]
        public async Task OnApplyTemplateCalculations_SmokeTestSucceeds()
        {
            OnApplyTemplateCalculations onApplyTemplateCalculations = new OnApplyTemplateCalculations(_logger,
                _applyTemplateCalculationsService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnApplyTemplateCalculations.FunctionName,
                ServiceBusConstants.QueueNames.ApplyTemplateCalculations,
                (Message smokeResponse) => onApplyTemplateCalculations.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task CalcsAddRelationshipToBuildProject_SmokeTestSucceeds()
        {
            CalcsAddRelationshipToBuildProject calcsAddRelationshipToBuildProject = new CalcsAddRelationshipToBuildProject(_logger,
                _buildProjectsService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(CalcsAddRelationshipToBuildProject.FunctionName,
                ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships,
                (Message smokeResponse) => calcsAddRelationshipToBuildProject.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnCalcsInstructAllocationResults_SmokeTestSucceeds()
        {
            OnCalcsInstructAllocationResults onCalcsInstructAllocationResults = new OnCalcsInstructAllocationResults(_logger,
                _buildProjectsService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnCalcsInstructAllocationResults.FunctionName,
                ServiceBusConstants.QueueNames.CalculationJobInitialiser, 
                (Message smokeResponse) => onCalcsInstructAllocationResults.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnCalculationAggregationsJobCompleted_SmokeTestSucceeds()
        {
            OnCalculationAggregationsJobCompleted onCalculationAggregationsJobCompleted = new OnCalculationAggregationsJobCompleted(_logger,
                _jobService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnCalculationAggregationsJobCompleted.FunctionName,
                ServiceBusConstants.TopicSubscribers.CreateInstructAllocationsJob,
                (Message smokeResponse) => onCalculationAggregationsJobCompleted.Run(smokeResponse),
                ServiceBusConstants.TopicNames.JobNotifications);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnDataDefinitionChanges_SmokeTestSucceeds()
        {
            OnDataDefinitionChanges onDataDefinitionChanges = new OnDataDefinitionChanges(_logger,
                _datasetDefinitionFieldChangesProcessor,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnDataDefinitionChanges.FunctionName,
                ServiceBusConstants.TopicSubscribers.UpdateCalculationFieldDefinitionProperties,
                (Message smokeResponse) => onDataDefinitionChanges.Run(smokeResponse),
                ServiceBusConstants.TopicNames.DataDefinitionChanges);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnDeleteCalculationResults_SmokeTestSucceeds()
        {
            OnDeleteCalculationResults onDeleteCalculationResults = new OnDeleteCalculationResults(_logger,
                _calculationService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnDeleteCalculationResults.FunctionName,
                ServiceBusConstants.QueueNames.DeleteCalculationResults,
                (Message smokeResponse) => onDeleteCalculationResults.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }
        
        [TestMethod]
        public async Task OnDeleteCalculations_SmokeTestSucceeds()
        {
            OnDeleteCalculations onDeleteCalculations = new OnDeleteCalculations(_logger,
                _calculationService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnDeleteCalculations.FunctionName,
                ServiceBusConstants.QueueNames.DeleteCalculations,
                (Message smokeResponse) => onDeleteCalculations.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
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
    }
}
