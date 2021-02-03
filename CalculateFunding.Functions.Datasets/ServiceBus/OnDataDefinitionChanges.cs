using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Processing.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnDataDefinitionChanges : Retriable
    {
        public const string FunctionName = "on-data-definition-changes";

        public OnDataDefinitionChanges(
            ILogger logger,
            IDatasetDefinitionNameChangeProcessor datasetDefinitionChangesProcessor,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, $"{ServiceBusConstants.TopicNames.DataDefinitionChanges}/{ServiceBusConstants.TopicSubscribers.UpdateDataDefinitionName}", useAzureStorage, userProfileProvider, datasetDefinitionChangesProcessor, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.DataDefinitionChanges,
            ServiceBusConstants.TopicSubscribers.UpdateDataDefinitionName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message);
        }
    }
}
