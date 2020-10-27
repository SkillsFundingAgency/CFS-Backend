using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public class OnTestSpecificationProviderResultsCleanup : Retriable
    {
        private readonly ILogger _logger;
        private readonly ITestResultsService _testResultsService;
        public const string FunctionName = "on-test-specification-provider-results-cleanup";

        public OnTestSpecificationProviderResultsCleanup(
            ILogger logger,
            ITestResultsService testResultsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, $"{ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup}/{ServiceBusConstants.TopicSubscribers.CleanupTestResultsForSpecificationProviders}" , useAzureStorage, userProfileProvider, testResultsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));

            _logger = logger;
            _testResultsService = testResultsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup,
            ServiceBusConstants.TopicSubscribers.CleanupTestResultsForSpecificationProviders,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(message,
                async () =>
                {
                    await _testResultsService.CleanupTestResultsForSpecificationProviders(message);
                });
        }
    }
}
