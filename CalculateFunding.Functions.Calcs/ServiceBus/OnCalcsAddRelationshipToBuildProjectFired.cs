using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class CalcsAddRelationshipToBuildProject
    {
        [FunctionName("on-calcs-add-data-relationship")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var buildProjectsService = scope.ServiceProvider.GetService<IBuildProjectsService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();
                ICacheProvider cacheProvider = scope.ServiceProvider.GetService<ICacheProvider>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await buildProjectsService.UpdateBuildProjectRelationships(message);
                           
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships}");
                    throw;
                }
                
            }
        }
    }
}
