using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public class OnDeleteTests
    {
        private readonly ILogger _logger;
        private readonly IScenariosService _scenariosService;

        public OnDeleteTests(
            ILogger logger,
            IScenariosService testEngineService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(testEngineService, nameof(testEngineService));

            _logger = logger;
            _scenariosService = testEngineService;
        }

        [FunctionName("on-delete-tests")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteTests, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
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
        }
    }
}
