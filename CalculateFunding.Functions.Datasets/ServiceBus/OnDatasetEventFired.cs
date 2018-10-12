using System;
using System.Globalization;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public static class OnDatasetEvent
    {
        [FunctionName("on-dataset-event")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.ProcessDataset, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");

            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (IServiceScope scope = IocConfig.Build(config).CreateScope())
            {
                IProcessDatasetService processDatasetService = scope.ServiceProvider.GetService<IProcessDatasetService>();
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                Serilog.ILogger logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await processDatasetService.ProcessDataset(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ProcessDataset}");
                    throw;
                }

            }
        }
    }
}
