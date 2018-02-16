using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Specs
{
    public static class OnSpecsTimerFired
    {
        [FunctionName("on-specs-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var messagePump = scope.ServiceProvider.GetService<IMessagePumpService>();
                var specificationsService = scope.ServiceProvider.GetService<ISpecificationsService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                await Task.WhenAll(
                   
                    messagePump.ReceiveAsync("spec-events", "spec-events-add-definition-relationship",
                        async message =>
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await specificationsService.AssignDataDefinitionRelationship(message);
                        },(exception) => {

                            logger.Error(exception, "An error occurred getting message from topic: spec-events for subscription: spec-events-update-search");
                        } )
                );
            }
        }
    }
}
