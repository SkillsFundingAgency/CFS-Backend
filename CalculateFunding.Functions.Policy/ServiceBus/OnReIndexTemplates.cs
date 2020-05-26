using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Policy.ServiceBus
{
    public class OnReIndexTemplates : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly ITemplatesReIndexerService _templatesReIndexerService;
        public const string FunctionName = "on-policy-reindex-templates";

        public OnReIndexTemplates(ILogger logger,
            ITemplatesReIndexerService templatesReIndexerService,
            IMessengerService messengerService,
             IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(templatesReIndexerService, nameof(templatesReIndexerService));

            _logger = logger;
            _templatesReIndexerService = templatesReIndexerService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.PolicyReIndexTemplates,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message)
        {
            await Run(async () =>
                {
                    try
                    {
                        await _templatesReIndexerService.Run(message);
                    }
                    catch (Exception exception)
                    {
                        _logger.Error(exception,
                            $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.PolicyReIndexTemplates}");
                    }
                },
                message);
        }
    }
}
