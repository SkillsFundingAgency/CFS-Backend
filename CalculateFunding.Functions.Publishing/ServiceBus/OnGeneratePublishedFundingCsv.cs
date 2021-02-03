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
    public class OnGeneratePublishedFundingCsv : Retriable
    {
        private const string FunctionName = "on-publishing-generate-published-funding-csv";
        private const string QueueName = ServiceBusConstants.QueueNames.GeneratePublishedFundingCsv;

        public OnGeneratePublishedFundingCsv(
            ILogger logger,
            IFundingLineCsvGenerator csvGenerator,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, csvGenerator, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.GeneratePublishedFundingCsv,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message)
        {
            await base.Run(message);
        }
    }
}