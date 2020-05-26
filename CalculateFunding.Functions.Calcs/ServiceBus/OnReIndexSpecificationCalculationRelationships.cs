using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
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
    public class OnReIndexSpecificationCalculationRelationships : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IReIndexSpecificationCalculationRelationships _reIndexSpecificationRelationships;
        public const string FunctionName = "on-reindex-specification-calculation-relationships";

        public OnReIndexSpecificationCalculationRelationships(
            ILogger logger,
            IReIndexSpecificationCalculationRelationships service,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(service, nameof(service));

            _logger = logger;
            _reIndexSpecificationRelationships = service;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.ReIndexSpecificationCalculationRelationships,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _reIndexSpecificationRelationships.Run(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ReIndexSpecificationCalculationRelationships}");
                    throw;
                }
            },
            message);
        }
    }
}
