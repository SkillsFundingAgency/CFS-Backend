using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Specs.ServiceBus
{
    public class OnAddRelationshipEvent : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly ISpecificationsService _specificationsService;
        public const string FunctionName = "on-add-relationship-event";

        public OnAddRelationshipEvent(
            ILogger logger,
            ISpecificationsService specificationsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationsService, nameof(specificationsService));

            _logger = logger;
            _specificationsService = specificationsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _specificationsService.AssignDataDefinitionRelationship(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification}");
                    throw;
                }
            },
            message);
        }
    }
}
