﻿using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Models;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Core;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Linq;

namespace CalculateFunding.Services.Processing.Functions
{
    public abstract class Retriable : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly string _path;
        private readonly IConfigurationRefresher _configurationRefresher;

        protected Retriable(ILogger logger,
            IMessengerService messengerService,
            string functionName,
            string path,
            bool useAzureStorage,
            IUserProfileProvider userProfileProvider,
            IProcessingService processingService,
            IConfigurationRefresherProvider refresherProvider) 
            : base(logger, messengerService, functionName, useAzureStorage, userProfileProvider, processingService)
        {
            _logger = logger;
            _path = path;

            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        protected override async Task Run(Message message, Func<Task> func = null)
        {
            try 
            {
                await _configurationRefresher.TryRefreshAsync();

                await base.Run(message, func);
            }
            catch (NonRetriableException nrEx)
            {
                _logger.Error(nrEx, $"An error occurred processing message on : {_path}");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred processing message on : {_path}");
                throw;
            }
        }
    }
}
