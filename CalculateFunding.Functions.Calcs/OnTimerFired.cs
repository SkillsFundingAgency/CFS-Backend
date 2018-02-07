using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Calcs
{
    public static class OnTimerFired
    {
        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var messagePump = scope.ServiceProvider.GetService<IMessagePumpService>();
                var calculationService = scope.ServiceProvider.GetService<ICalculationService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                await Task.WhenAll(
                   
                    messagePump.ReceiveAsync("calc-events", "calc-events-create-draft",
                        async message =>
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await calculationService.CreateCalculation(message);
                        },(exception) => {

                            logger.Error(exception, "An error occurred getting message from topic: calc-events for subscription: calc-events-create-draft");
                        } )
                );
            }
        }
    }
}
