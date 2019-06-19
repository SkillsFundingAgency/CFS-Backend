using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Policy
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

        public static void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<IPolicyRepository, PolicyRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                cosmosDbSettings.CollectionName = "policy";

                config.Bind("CosmosDbSettings", cosmosDbSettings);

                CosmosRepository cosmosRepostory = new CosmosRepository(cosmosDbSettings);

                return new PolicyRepository(cosmosRepostory);
            });

            builder.AddApplicationInsights(config, "CalculateFunding.Functions.Policy");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Policy");

            builder.AddLogging("CalculateFunding.Functions.Policy");

            builder.AddTelemetry();
        }
    }
}
