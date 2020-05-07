using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public class OnDeleteTests : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IScenariosService _scenariosService;
        public const string FunctionName = "on-delete-tests";

        public OnDeleteTests(
            ILogger logger,
            IScenariosService scenariosService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scenariosService, nameof(scenariosService));

            _logger = logger;
            _scenariosService = scenariosService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteTests, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _scenariosService.DeleteTests(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred processing message on queue: '{ServiceBusConstants.QueueNames.DeleteTests}'");
                    throw;
                }
            },
            message);
        }
    }
}
