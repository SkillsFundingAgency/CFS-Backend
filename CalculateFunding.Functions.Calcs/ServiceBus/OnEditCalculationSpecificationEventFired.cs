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
    public class OnEditCalculationSpecificationEvent
    {
        private readonly ILogger _logger;
        private readonly ICalculationService _calculationService;

        public OnEditCalculationSpecificationEvent(
            ILogger logger,
            ICalculationService calculationService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));

            _logger = logger;
            _calculationService = calculationService;
        }

        [FunctionName("on-edit-calculation-for-calcs")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditCalculation,
            ServiceBusConstants.TopicSubscribers.UpdateCalculationsForEditCalculation,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _calculationService.UpdateCalculationsForCalculationSpecificationChange(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.EditCalculation}");
                throw;
            }
        }
    }
}
