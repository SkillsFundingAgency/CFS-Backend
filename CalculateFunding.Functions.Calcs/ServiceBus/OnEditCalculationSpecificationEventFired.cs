using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnEditCalculationSpecificationEvent
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ICalculationService _calculationService;

        public OnEditCalculationSpecificationEvent(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            ICalculationService calculationService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
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
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
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
