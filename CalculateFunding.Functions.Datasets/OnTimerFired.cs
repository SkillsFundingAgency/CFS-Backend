using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Datasets
{
    public static class OnTimerFired
    {
        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var messagePump = scope.ServiceProvider.GetService<IMessagePumpService>();
                var datasetService = scope.ServiceProvider.GetService<IDatasetService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                await Task.WhenAll(
                   
                    messagePump.ReceiveAsync("dataset-events", "dataset-events-datasets",
                        async message =>
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await datasetService.ProcessDataset(message);
                        },(exception) => {

                            logger.Error(exception, "An error occurred getting message from topic: dataset-events for subscription: dataset-events-datasets");
                        } )
				);
            }
        }
    }
}
