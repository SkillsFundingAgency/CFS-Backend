using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnPublishedFundingUndo : SmokeTest
    {
        private const string FunctionName = "on-published-funding-undo";
        private const string QueueName = ServiceBusConstants.QueueNames.PublishedFundingUndo;
        
        private readonly ILogger _logger;
        private readonly IPublishedFundingUndoJobService _undoService;

        public OnPublishedFundingUndo(
            ILogger logger,
            IPublishedFundingUndoJobService undoService,
            IMessengerService messengerService,
            bool useAzureStorage = false) : base(logger, messengerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(undoService, nameof(undoService));

            _logger = logger;
            _undoService = undoService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _undoService.Run(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"Job threw non retriable exception: {QueueName}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {QueueName}");
                    
                    throw;
                }
            },
            message);
        }
    }
}
