using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Specs.ServiceBus
{
    public static class OnAddRelatioshipEvent
    {
        [FunctionName("on-add-relationship-event")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            var config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                var specificationsService = scope.ServiceProvider.GetService<ISpecificationsService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {

                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await specificationsService.AssignDataDefinitionRelationship(message);

                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification}");
                    throw;
                }
            }
        }
    }
}
