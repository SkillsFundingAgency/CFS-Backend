using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnSearchIndexWriterEventTrigger : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly ISearchIndexWriterService _searchIndexWriterService;
        public const string FunctionName = FunctionConstants.SearchIndexWriter;
        public const string QueueName = ServiceBusConstants.QueueNames.SearchIndexWriter;

        public OnSearchIndexWriterEventTrigger(ILogger logger,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            ISearchIndexWriterService searchIndexWriterService,
            bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(searchIndexWriterService, nameof(searchIndexWriterService));

            _logger = logger;
            _searchIndexWriterService = searchIndexWriterService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _searchIndexWriterService.CreateSearchIndex(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.MapFdzDatasets}");
                    throw;
                }
            },
            message);
        }
    }
}
