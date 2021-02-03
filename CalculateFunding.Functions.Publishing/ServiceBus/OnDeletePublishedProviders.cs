using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnDeletePublishedProviders : Retriable
    {
        public const string FunctionName = "on-publishing-delete-published-providers";
        private const string QueueName = ServiceBusConstants.QueueNames.DeletePublishedProviders;

        public OnDeletePublishedProviders(
            ILogger logger,
            IDeletePublishedProvidersService deletePublishedProvidersService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, deletePublishedProvidersService, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message)
        {
            await base.Run(message);
        }
    }
}
