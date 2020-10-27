using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Specs.ServiceBus
{
    public class OnDeleteSpecifications : Retriable
    {
        private readonly ILogger _logger;
        private readonly ISpecificationsService _specificationsService;
        public const string FunctionName = "on-delete-specifications";
        private const string QueueName = ServiceBusConstants.QueueNames.DeleteSpecifications;

        public OnDeleteSpecifications(
            ILogger logger,
            ISpecificationsService specificationsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, specificationsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationsService, nameof(specificationsService));

            _logger = logger;
            _specificationsService = specificationsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(QueueName, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(message,
                async () =>
                {
                    await _specificationsService.DeleteSpecification(message);
                });
        }
    }
}
