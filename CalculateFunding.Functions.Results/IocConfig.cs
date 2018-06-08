using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Functions.Results
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

            builder.AddScoped<ICalculationResultsRepository, CalculationResultsRepository>();
            builder.AddScoped<IResultsService, ResultsService>();
	        builder.AddScoped<IResultsSearchService, ResultsSearchService>();
            builder.AddScoped<ICalculationProviderResultsSearchService, CalculationProviderResultsSearchService>();

            MapperConfiguration resultsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            builder
                .AddSingleton(resultsConfig.CreateMapper());

            builder.AddInterServiceClient(config);

            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.CollectionName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new CalculationResultsRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IProviderSourceDatasetRepository, ProviderSourceDatasetRepository>((ctx) =>
            {
                CosmosDbSettings provDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", provDbSettings);

                provDbSettings.CollectionName = "providersources";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(provDbSettings);

                return new ProviderSourceDatasetRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IPublishedProviderResultsRepository, PublishedProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings resultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", resultsDbSettings);

                resultsDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository resultsRepostory = new CosmosRepository(resultsDbSettings);

                return new PublishedProviderResultsRepository(resultsRepostory);
            });

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IPublishedProviderResultsAssemblerService, PublishedProviderResultsAssemblerService>();

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config);
            builder.AddLogging("CalculateFunding.Functions.Results");
            builder.AddTelemetry();

            builder.AddInterServiceClient(config);

            builder.AddPolicySettings(config);

            builder.AddSingleton<IResultsResilliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                ResiliencePolicies resiliencePolicies = new ResiliencePolicies()
                {
                    CalculationProviderResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    ResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    SpecificationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };

                return resiliencePolicies;
            });
        }
    }
}
