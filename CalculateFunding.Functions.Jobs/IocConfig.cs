using System;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Services.Jobs.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Jobs
{
    public static class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(config);
            }

            return _serviceProvider;
        }

        public static IServiceProvider BuildServiceProvider(IConfigurationRoot config)
        {
            ServiceCollection serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                .AddSingleton<IJobManagementService, JobManagementService>();

            builder
                .AddSingleton<IJobRepository, JobRepository>((ctx) =>
                {
                    CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                    config.Bind("CosmosDbSettings", cosmosDbSettings);

                    cosmosDbSettings.CollectionName = "jobs";

                    CosmosRepository jobCosmosRepostory = new CosmosRepository(cosmosDbSettings);

                    return new JobRepository(jobCosmosRepostory);
                });

            builder.AddServiceBus(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Jobs");

            builder.AddLogging("CalculateFunding.Functions.Jobs");

            builder.AddTelemetry();
        }
    }
}
