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
    public class OnCalculationResultsCsvGeneration : Retriable
    {
        private readonly ILogger _logger;
        private readonly IProviderResultsCsvGeneratorService _csvGeneratorService;
        private const string FunctionName = "on-calculation-results-csv-generation";
        private const string QueueName = ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration;

        public OnCalculationResultsCsvGeneration(
            ILogger logger,
            IProviderResultsCsvGeneratorService csvGeneratorService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, csvGeneratorService)
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

