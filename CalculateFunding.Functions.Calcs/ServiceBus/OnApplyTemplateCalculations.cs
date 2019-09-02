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
    public class OnApplyTemplateCalculations
    {
        private readonly ILogger _logger;
        private readonly IApplyTemplateCalculationsService _templateCalculationsService;

        public OnApplyTemplateCalculations(
            ILogger logger,
            IApplyTemplateCalculationsService templateCalculationsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(templateCalculationsService, nameof(templateCalculationsService));

            _logger = logger;
            _templateCalculationsService = templateCalculationsService;
        }

        [FunctionName("on-apply-template-calculations")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.ApplyTemplateCalculations,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey, IsSessionsEnabled = true)] Message message)
        {
            try
            {
                await _templateCalculationsService.ApplyTemplateCalculation(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ApplyTemplateCalculations}");
                throw;
            }
        }
    }
}
