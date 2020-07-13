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
    public class OnMergeSpecificationInformationForProviderWithResults : SmokeTest
    {
        private const string FunctionName = "on-merge-specification-information-for-provider-with-results";
        private const string QueueName = ServiceBusConstants.QueueNames.MergeSpecificationInformationForProvider;

        private readonly ISpecificationsWithProviderResultsService _service;
        private readonly ILogger _logger;

        public OnMergeSpecificationInformationForProviderWithResults(
            ILogger logger,
            ISpecificationsWithProviderResultsService service,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(service, nameof(service));

            _logger = logger;
            _service = service;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(QueueName, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _service.MergeSpecificationInformation(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {QueueName}");
                
                    throw;
                }
            },
            message);
        }
    }
}

