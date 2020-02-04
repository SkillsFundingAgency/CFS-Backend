using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.ServiceBus
{
    public class OnScaleUpCosmosDbCollection : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;
        public const string FunctionName = "on-scale-up-cosmosdb-collection";

        public OnScaleUpCosmosDbCollection(
           ILogger logger,
           ICosmosDbScalingService scalingService,
           IMessengerService messegerService,
           bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scalingService, nameof(scalingService));

            _logger = logger;
            _scalingService = scalingService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.JobNotifications,
            ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _scalingService.ScaleUp(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection}");
                    throw;
                }
            },
            message);
        }
    }
}
