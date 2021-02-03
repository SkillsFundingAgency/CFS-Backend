using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Interfaces;
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

namespace CalculateFunding.Functions.Results.SmokeTests
{
    [TestClass]
    public class ResultsFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IProviderResultsCsvGeneratorService _providerResultsCsvGeneratorService;
        private static IResultsService _resultsService;
        private static IProviderCalculationResultsReIndexerService _providerCalculationResultsReIndexerService;
        private static ISpecificationsWithProviderResultsService _providerResultsService;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("results");

            _logger = CreateLogger();
            _providerResultsCsvGeneratorService = CreateProviderResultsCsvGeneratorService();
            _resultsService = CreateResultsService();
            _providerCalculationResultsReIndexerService = CreateProviderCalculationResultsReIndexerService();
            _userProfileProvider = CreateUserProfileProvider();
            _providerResultsService = Substitute.For<ISpecificationsWithProviderResultsService>();
        }

        [TestMethod]
        public async Task OnCalculationResultsCsvGeneration_SmokeTestSucceeds()
        {
            OnCalculationResultsCsvGeneration onCalculationResultsCsvGeneration = new OnCalculationResultsCsvGeneration(_logger,
                _providerResultsCsvGeneratorService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                 IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration,
                async(Message smokeResponse) => await onCalculationResultsCsvGeneration.Run(smokeResponse), useSession:true);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnProviderResultsSpecificationCleanup_SmokeTestSucceeds()
        {
            OnProviderResultsSpecificationCleanup onProviderResultsSpecificationCleanup = new OnProviderResultsSpecificationCleanup(_logger,
                _resultsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                 IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.CleanupCalculationResultsForSpecificationProviders,
                async(Message smokeResponse) => await onProviderResultsSpecificationCleanup.Run(smokeResponse),
                ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup);

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnReIndexCalculationResults_SmokeTestSucceeds()
        {
            OnReIndexCalculationResults onReIndexCalculationResults = new OnReIndexCalculationResults(_logger,
                _providerCalculationResultsReIndexerService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                 IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex,
                async(Message smokeResponse) => await onReIndexCalculationResults.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }
        
        [TestMethod]
        public async Task OnMergeSpecificationInformationForProviderWithResults_SmokeTestSucceeds()
        {
            OnMergeSpecificationInformationForProviderWithResults onReIndexCalculationResults = new OnMergeSpecificationInformationForProviderWithResults(_logger,
                _providerResultsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.MergeSpecificationInformationForProvider,
                async(Message smokeResponse) => await onReIndexCalculationResults.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnDeleteCalculationResults_SmokeTestSucceeds()
        {
            OnDeleteCalculationResults onDeleteCalculationResults = new OnDeleteCalculationResults(_logger,
                _resultsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteCalculationResults,
                async(Message smokeResponse) => await onDeleteCalculationResults.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
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

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
