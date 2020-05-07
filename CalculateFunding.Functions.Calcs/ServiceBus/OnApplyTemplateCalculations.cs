using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnApplyTemplateCalculations : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IApplyTemplateCalculationsService _templateCalculationsService;
        public const string FunctionName = "on-apply-template-calculations";

        public OnApplyTemplateCalculations(
            ILogger logger,
            IApplyTemplateCalculationsService templateCalculationsService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(templateCalculationsService, nameof(templateCalculationsService));

            _logger = logger;
            _templateCalculationsService = templateCalculationsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.ApplyTemplateCalculations,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey, IsSessionsEnabled = true)] Message message)
        {
            await Run(async () =>
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
            },
            message);
        }
    }
}
