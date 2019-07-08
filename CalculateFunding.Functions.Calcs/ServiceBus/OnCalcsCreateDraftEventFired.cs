using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnCalcsCreateDraftEvent
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ICalculationService _calculationService;

        public OnCalcsCreateDraftEvent(
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

        [FunctionName("on-calcs-create-draft-event")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CreateDraftCalculation, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
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
