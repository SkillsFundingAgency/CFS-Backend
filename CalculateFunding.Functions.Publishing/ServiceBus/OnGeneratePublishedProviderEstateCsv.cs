using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnGeneratePublishedProviderEstateCsv : Retriable
    {
        private readonly ILogger _logger;
        private readonly IPublishedProviderEstateCsvGenerator _csvGenerator;

        private const string FunctionName = "on-publishing-generate-published-provider-estate-csv";
        private const string QueueName = ServiceBusConstants.QueueNames.GeneratePublishedProviderEstateCsv;

        public OnGeneratePublishedProviderEstateCsv(
            ILogger logger,
            IPublishedProviderEstateCsvGenerator csvGenerator,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, csvGenerator)
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
