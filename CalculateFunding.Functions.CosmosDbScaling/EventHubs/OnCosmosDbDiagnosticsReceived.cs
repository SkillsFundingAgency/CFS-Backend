using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventHubs;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Extensions.Configuration;
using CalculateFunding.Services.Core.Extensions;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;

namespace CalculateFunding.Functions.CosmosDbScaling.EventHubs
{
    public static class OnCosmosDbDiagnosticsReceived
    {
        //[FunctionName("OnCosmosDbDiagnosticsReceived")]
        //public static async Task Run([EventHubTrigger(EventHubsConstants.Hubs.CosmosDbDiagnostics, Connection = EventHubsConstants.ConnectionStringConfigurationKey)] EventData[] events)
        //{
        //    IConfigurationRoot config = ConfigHelper.AddConfig();

        //    using (IServiceScope scope = IocConfig.Build(config).CreateScope())
        //    {
        //        ILogger logger = scope.ServiceProvider.GetService<ILogger>();
        //        logger.Information("Scope created, starting to generate allocations");
        //        ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
        //        ICosmosDbScalingService scalingService = scope.ServiceProvider.GetService<ICosmosDbScalingService>();

        //        try
        //        {
        //            correlationIdProvider.SetCorrelationId(Guid.NewGuid().ToString());

        //            await scalingService.ScaleUp(events);

        //            logger.Information("Generate allocations complete");
        //        }
        //        catch (NonRetriableException nrEx)
        //        {
        //            logger.Error(nrEx, $"An error occurred processing messages on event hub: {EventHubsConstants.Hubs.CosmosDbDiagnostics}");
        //        }
        //        catch (Exception exception)
        //        {
        //            logger.Error(exception, $"An error occurred processing messages on event hub: {EventHubsConstants.Hubs.CosmosDbDiagnostics}");
        //            throw;
        //        }
        //    }
        //}
    }
}
