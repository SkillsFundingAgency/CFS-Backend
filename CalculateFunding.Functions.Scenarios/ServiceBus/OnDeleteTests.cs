using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public class OnDeleteTests : Retriable
    {
        private readonly IScenariosService _scenariosService;
        public const string FunctionName = "on-delete-tests";
        public const string QueueName = ServiceBusConstants.QueueNames.DeleteTests;

        public OnDeleteTests(
            ILogger logger,
            IScenariosService scenariosService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, scenariosService, refresherProvider)
        {
            Guard.ArgumentNotNull(scenariosService, nameof(scenariosService));

            _scenariosService = scenariosService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(message,
                async () =>
                {
                    await _scenariosService.DeleteTests(message);
                }
            );
        }
    }
}
