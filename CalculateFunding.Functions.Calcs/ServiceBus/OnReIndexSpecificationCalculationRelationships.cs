using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnReIndexSpecificationCalculationRelationships : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IReIndexSpecificationCalculationRelationships _reIndexSpecificationRelationships;
        public const string FunctionName = "on-reindex-specification-calculation-relationships";

        public OnReIndexSpecificationCalculationRelationships(
            ILogger logger,
            IReIndexSpecificationCalculationRelationships service,
            IMessengerService messengerService,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(service, nameof(service));

            _logger = logger;
            _reIndexSpecificationRelationships = service;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.ReIndexSpecificationCalculationRelationships,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _reIndexSpecificationRelationships.Run(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.DeleteCalculations}");
                    throw;
                }
            },
            message);
        }
    }
}
