using CalculateFunding.Services.CalcEngine.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Runners.CalcEngine
{
    public class OnCalcsGenerateAllocationResults : ServiceBusQueueWorker<Message>
    {
        private readonly ICalculationEngineService _calculationEngineService;

        public OnCalcsGenerateAllocationResults(IConfiguration configuration,
            ICalculationEngineService calculationEngineService, IHostApplicationLifetime hostApplicationLifetime, ILogger<OnCalcsGenerateAllocationResults> logger)
            : base(configuration, hostApplicationLifetime, logger)
        {
            _calculationEngineService = calculationEngineService;
        }

        protected override async Task ProcessMessage(Message message, string messageId, IReadOnlyDictionary<string, object> userProperties, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Processing message {message}", message);

            await _calculationEngineService.Process(message);

            Logger.LogInformation("Message {message} processed", message);
        }
    }
}