using System;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Bulkhead;
using Serilog;

namespace CalculateFunding.Functions.CalcEngine
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build()
        {
            if (_serviceProvider == null)
                _serviceProvider = BuildServiceProvider();

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider()
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            builder.AddSingleton<ICalculationEngineService, CalculationEngineService>();
            builder.AddSingleton<ICalculationEngine, CalculationEngine>();
            builder.AddSingleton<IAllocationFactory, AllocationFactory>();

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>((ctx) =>
            {
                CosmosDbSettings providerSourceDatasetsCosmosSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", providerSourceDatasetsCosmosSettings);

                providerSourceDatasetsCosmosSettings.CollectionName = "providersourcedatasets";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(providerSourceDatasetsCosmosSettings);

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                return new ProviderSourceDatasetsRepository(calcsCosmosRepostory, engineSettings);
            });

            builder.AddSingleton<IProviderResultsRepository, ProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings calcResultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcResultsDbSettings);

                calcResultsDbSettings.CollectionName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calcResultsDbSettings);

                ISearchRepository<CalculationProviderResultsIndex> calculationProviderResultsSearchRepository = ctx.GetService<ISearchRepository<CalculationProviderResultsIndex>>();

                ISpecificationsRepository specificationsRepository = ctx.GetService<ISpecificationsRepository>();

                ILogger logger = ctx.GetService<ILogger>();

                return new ProviderResultsRepository(calcsCosmosRepostory, calculationProviderResultsSearchRepository, specificationsRepository, logger);
            });

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
                .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder.AddCalcsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);

            builder.AddEngineSettings(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config);

            builder.AddLogging("CalculateFunding.Functions.CalcEngine");

            builder.AddTelemetry();

            builder.AddSearch(config);

            builder.AddPolicySettings(config);

            builder.AddSingleton<ICalculatorResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                CalculatorResiliencePolicies resiliencePolicies = new CalculatorResiliencePolicies()
                {
                    ProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ProviderSourceDatasetsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                    Messenger = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                    CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };

                return resiliencePolicies;
            });
        }
    }
}
