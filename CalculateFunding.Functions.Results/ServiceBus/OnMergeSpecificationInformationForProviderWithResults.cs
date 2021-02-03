using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnMergeSpecificationInformationForProviderWithResults : Retriable
    {
        private const string FunctionName = "on-merge-specification-information-for-provider-with-results";
        private const string QueueName = ServiceBusConstants.QueueNames.MergeSpecificationInformationForProvider;

        public OnMergeSpecificationInformationForProviderWithResults(
            ILogger logger,
            ISpecificationsWithProviderResultsService service,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, service, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(QueueName, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message)
        {
            await base.Run(message);
        }
    }
}

