using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnCalcsCreateDraftEvent
    {
        private readonly ILogger _logger;
        private readonly ICalculationService _calculationService;

        public OnCalcsCreateDraftEvent(
            ILogger logger,
            ICalculationService calculationService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));

            _logger = logger;
            _calculationService = calculationService;
        }

        [FunctionName("on-calcs-create-draft-event")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CreateDraftCalculation, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _calculationService.CreateCalculation(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.CreateDraftCalculation}");
                throw;
            }
        }
    }
}
