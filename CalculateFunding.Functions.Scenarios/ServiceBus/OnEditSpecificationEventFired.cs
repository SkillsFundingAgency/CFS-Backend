using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public class OnEditSpecificationEvent : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IScenariosService _scenariosService;
        public const string FunctionName = "on-edit-specification";

        public OnEditSpecificationEvent(
            ILogger logger,
            IScenariosService scenariosService,
            IMessengerService messegerService,
            bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scenariosService, nameof(scenariosService));

            _logger = logger;
            _scenariosService = scenariosService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditSpecification,
            ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditSpecification,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _scenariosService.UpdateScenarioForSpecification(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.EditSpecification}");
                    throw;
                }
            },
            message);
        }
    }
}
