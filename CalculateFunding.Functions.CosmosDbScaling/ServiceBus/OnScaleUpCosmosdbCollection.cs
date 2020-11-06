using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.ServiceBus
{
    public class OnScaleUpCosmosDbCollection : Retriable
    {
        public const string FunctionName = "on-scale-up-cosmosdb-collection";

        public OnScaleUpCosmosDbCollection(
           ILogger logger,
           ICosmosDbScalingService scalingService,
           IMessengerService messengerService,
           IUserProfileProvider userProfileProvider, bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, $"{ServiceBusConstants.TopicNames.JobNotifications}/{ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection}", useAzureStorage, userProfileProvider, scalingService)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.JobNotifications,
            ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message);
        }
    }
}
