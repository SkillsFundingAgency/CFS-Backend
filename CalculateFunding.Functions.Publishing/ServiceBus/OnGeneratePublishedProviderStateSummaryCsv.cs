using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnGeneratePublishedProviderStateSummaryCsv : Retriable
    {
        private readonly ILogger _logger;
        private readonly IPublishedProviderStateSummaryCsvGenerator _csvGenerator;

        private const string FunctionName = "on-publishing-generate-published-provider-state-summary-csv";
        private const string QueueName = ServiceBusConstants.QueueNames.GeneratePublishedProviderStateSummaryCsv;

        public OnGeneratePublishedProviderStateSummaryCsv(
            ILogger logger,
            IPublishedProviderStateSummaryCsvGenerator csvGenerator,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, csvGenerator, refresherProvider)
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
