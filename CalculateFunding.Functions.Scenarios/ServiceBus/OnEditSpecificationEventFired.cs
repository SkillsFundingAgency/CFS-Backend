using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public class OnEditSpecificationEvent
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IScenariosService _scenariosService;

        public OnEditSpecificationEvent(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            IScenariosService scenariosService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(scenariosService, nameof(scenariosService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _scenariosService = scenariosService;
        }

        [FunctionName("on-edit-specification")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditSpecification,
            ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditSpecification,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _scenariosService.UpdateScenarioForSpecification(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.EditSpecification}");
                throw;
            }
        }
    }
}
