using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Linq;

namespace CalculateFunding.Services.Processing.Functions
{
    public abstract class Failure
    {
        private readonly ILogger _logger;
        private readonly string _queueName;
        private readonly IDeadletterService _jobHelperService;
        private readonly IConfigurationRefresher _configurationRefresher;

        protected Failure(
            ILogger logger,
            IDeadletterService jobHelperService,
            string queueName,
            IConfigurationRefresherProvider refresherProvider)
        {
            _logger = logger;
            _queueName = queueName;
            _jobHelperService = jobHelperService;

            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        protected async Task Process(Message message)
        {
            _logger.Information($"Scope created, starting to process dead letter message for {_queueName}");

            try
            {
                await _configurationRefresher.TryRefreshAsync();

                await _jobHelperService.Process(message);

                _logger.Information($"Proccessed {_queueName} dead lettered message complete");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred processing message on queue: {_queueName}");
                throw;
            }
        }
    }
}
