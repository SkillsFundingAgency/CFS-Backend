using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Models;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Threading.Tasks;
using CalculateFunding.Services.DeadletterProcessor;

namespace CalculateFunding.Services.Core.Functions
{
    public abstract class Failure
    {
        private readonly ILogger _logger;
        private readonly string _queueName;
        private readonly IJobHelperService _jobHelperService;

        protected Failure(
            ILogger logger,
            IJobHelperService jobHelperService,
            string queueName)
        {
            _logger = logger;
            _queueName = queueName;
            _jobHelperService = jobHelperService;
        }

        protected async Task Process(Message message)
        {
            _logger.Information($"Scope created, starting to process dead letter message for {_queueName}");

            try
            {
                await _jobHelperService.ProcessDeadLetteredMessage(message);

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
