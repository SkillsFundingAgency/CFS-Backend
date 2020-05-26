using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnProviderResultsSpecificationCleanup : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IResultsService _resultsService;
        public const string FunctionName = "on-provider-results-specification-cleanup";

        public OnProviderResultsSpecificationCleanup(
            ILogger logger,
            IResultsService resultsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));

            _logger = logger;
            _resultsService = resultsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup,
            ServiceBusConstants.TopicSubscribers.CleanupCalculationResultsForSpecificationProviders,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _resultsService.CleanupProviderResultsForSpecification(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup}");
                    throw;
                }
            },
            message);
        }
    }
}
