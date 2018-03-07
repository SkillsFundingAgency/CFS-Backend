using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Calcs
{
    public static class OnCalcsTimerFired
    {
        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var messagePump = scope.ServiceProvider.GetService<IMessagePumpService>();
                var calculationService = scope.ServiceProvider.GetService<ICalculationService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var buildProjectsService = scope.ServiceProvider.GetService<IBuildProjectsService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                await Task.WhenAll(

                    messagePump.ReceiveAsync("calc-events", "calc-events-create-draft",
                        async message =>
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await calculationService.CreateCalculation(message);
                        }, (exception) =>
                        {
                            logger.Error(exception, "An error occurred getting message from topic: calc-events for subscription: calc-events-create-draft");
                        }),
                    messagePump.ReceiveAsync("calc-events", "calc-events-instruct-generate-allocations",
                        async message =>
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await buildProjectsService.GenerateAllocationsInstruction(message);
                        }, (exception) => {

                            logger.Error(exception, "An error occurred getting message from topic: calc-events for subscription: calc-events-generate-allocations");
                        }, ReceiveMode.ReceiveAndDelete),

                     messagePump.ReceiveAsync("calc-events", "calcs-events-generate-allocation-results",
                        async message =>
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await buildProjectsService.UpdateAllocations(message);
                            
                        }, (exception) =>
                        {
                            logger.Error(exception, "An error occurred getting message from topic: calc-events for subscription: calcs-events-generate-allocation-results");
                        })
                );
            }
        }
    }
}
