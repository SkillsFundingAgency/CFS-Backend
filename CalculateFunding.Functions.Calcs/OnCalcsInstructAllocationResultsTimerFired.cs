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
    public static class OnCalcsInstructAllocationResultsTimerFired
    {
        [FunctionName("on-calcs-instruct-allocations-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var messagePump = scope.ServiceProvider.GetService<IMessagePumpService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var buildProjectsService = scope.ServiceProvider.GetService<IBuildProjectsService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();


                await messagePump.ReceiveAsync("calc-events", "calc-events-instruct-generate-allocations",
                         async message =>
                         {
                             correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                             await buildProjectsService.GenerateAllocationsInstruction(message);
                         }, (exception) =>
                         {

                             logger.Error(exception, "An error occurred getting message from topic: calc-events for subscription: calc-events-generate-allocations");
                         });
            }
        }
    }
}
