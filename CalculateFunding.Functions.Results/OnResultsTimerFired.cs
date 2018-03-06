using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Results
{
    public static class OnResultsTimerFired
    {
        [FunctionName("on-results-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var messagePump = scope.ServiceProvider.GetService<IMessagePumpService>();
                var resultsService = scope.ServiceProvider.GetService<IResultsService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                await Task.WhenAll(

                    messagePump.ReceiveAsync("dataset-events", "dataset-events-results",
                        async message =>
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());

                            await resultsService.UpdateProviderData(message);
                        }, (exception) =>
                        {

                            logger.Error(exception, "An error occurred getting message from topic: dataset-events for subscription: calc-events-results");
                        })
                );
            }
        }
    }
}
