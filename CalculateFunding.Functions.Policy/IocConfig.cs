using System;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;

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
            builder
               .AddSingleton<IPolicyRepository, PolicyRepository>((ctx) =>
               {
                   CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                   cosmosDbSettings.ContainerName = "policy";

                   config.Bind("CosmosDbSettings", cosmosDbSettings);

                   CosmosRepository cosmosRepostory = new CosmosRepository(cosmosDbSettings);

                   return new PolicyRepository(cosmosRepostory);
               })
               .AddSingleton<IHealthChecker, FundingStreamService>();

            builder.AddSingleton<IPolicyResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                Polly.Policy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new PolicyResiliencePolicies()
                {
                    PolicyRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = redisPolicy
                };
            });

            builder.AddSingleton<ITemplateMetadataResolver>((ctx) =>
            {
                TemplateMetadataResolver resolver = new TemplateMetadataResolver();

                TemplateMetadataSchema10.TemplateMetadataGenerator schema10Generator = new TemplateMetadataSchema10.TemplateMetadataGenerator(ctx.GetService<ILogger>());

                resolver.Register("1.0", schema10Generator);

                return resolver;
            });

            builder.AddPolicySettings(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Policy");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Policy");

            builder.AddLogging("CalculateFunding.Functions.Policy");

            builder.AddTelemetry();
        }
    }
}
