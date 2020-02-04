using CalculateFunding.Common.Models;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Results.SmokeTests
{
    [TestClass]
    public class ResultsFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IProviderResultsCsvGeneratorService _providerResultsCsvGeneratorService;
        private static IResultsService _resultsService;
        private static IProviderCalculationResultsReIndexerService _providerCalculationResultsReIndexerService;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("results");

            _logger = CreateLogger();
            _providerResultsCsvGeneratorService = CreateProviderResultsCsvGeneratorService();
            _resultsService = CreateResultsService();
            _providerCalculationResultsReIndexerService = CreateProviderCalculationResultsReIndexerService();
        }

        [TestMethod]
        public async Task OnCalculationResultsCsvGeneration_SmokeTestSucceeds()
        {
            OnCalculationResultsCsvGeneration onCalculationResultsCsvGeneration = new OnCalculationResultsCsvGeneration(_logger,
                _providerResultsCsvGeneratorService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnCalculationResultsCsvGeneration.FunctionName,
                ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration,
                (Message smokeResponse) => onCalculationResultsCsvGeneration.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnProviderResultsSpecificationCleanup_SmokeTestSucceeds()
        {
            OnProviderResultsSpecificationCleanup onProviderResultsSpecificationCleanup = new OnProviderResultsSpecificationCleanup(_logger,
                _resultsService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnProviderResultsSpecificationCleanup.FunctionName,
                ServiceBusConstants.TopicSubscribers.CleanupCalculationResultsForSpecificationProviders,
                (Message smokeResponse) => onProviderResultsSpecificationCleanup.Run(smokeResponse),
                ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnReIndexCalculationResults_SmokeTestSucceeds()
        {
            OnReIndexCalculationResults onReIndexCalculationResults = new OnReIndexCalculationResults(_logger,
                _providerCalculationResultsReIndexerService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnReIndexCalculationResults.FunctionName,
                ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex,
                (Message smokeResponse) => onReIndexCalculationResults.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static IProviderResultsCsvGeneratorService CreateProviderResultsCsvGeneratorService()
        {
            return Substitute.For<IProviderResultsCsvGeneratorService>();
        }

        private static IResultsService CreateResultsService()
        {
            return Substitute.For<IResultsService>();
        }
        
        private static IProviderCalculationResultsReIndexerService CreateProviderCalculationResultsReIndexerService()
        {
            return Substitute.For<IProviderCalculationResultsReIndexerService>();
        }
    }
}
