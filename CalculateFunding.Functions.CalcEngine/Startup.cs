using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine.Validators;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using FluentValidation;
using Polly.Bulkhead;
using Serilog;
using CalculateFunding.Functions.CalcEngine.ServiceBus;
using System;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.CalcEngine.Startup))]

namespace CalculateFunding.Functions.CalcEngine
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<OnCalcsGenerateAllocationResults>();
            builder.AddSingleton<OnCalculationGenerateFailure>();
            builder.AddSingleton<ICalculationEngineService, CalculationEngineService>();
            builder.AddSingleton<ICalculationEngine, CalculationEngine>();
            builder.AddSingleton<IAllocationFactory, AllocationFactory>();
            builder.AddSingleton<IJobHelperService, JobHelperService>();

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>((ctx) =>
            {
                CosmosDbSettings providerSourceDatasetsCosmosSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", providerSourceDatasetsCosmosSettings);

                providerSourceDatasetsCosmosSettings.CollectionName = "providerdatasets";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(providerSourceDatasetsCosmosSettings);

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                return new ProviderSourceDatasetsRepository(calcsCosmosRepostory, engineSettings);
            });

            builder.AddSingleton<Services.Calculator.Interfaces.IProviderResultsRepository, Services.CalcEngine.ProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings calcResultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcResultsDbSettings);

                calcResultsDbSettings.CollectionName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calcResultsDbSettings);

                ISearchRepository<CalculationProviderResultsIndex> calculationProviderResultsSearchRepository = ctx.GetService<ISearchRepository<CalculationProviderResultsIndex>>();

                ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository = ctx.GetService<ISearchRepository<ProviderCalculationResultsIndex>>();

                ISpecificationsRepository specificationsRepository = ctx.GetService<ISpecificationsRepository>();

                ILogger logger = ctx.GetService<ILogger>();

                IFeatureToggle featureToggle = ctx.GetService<IFeatureToggle>();

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                return new Services.CalcEngine.ProviderResultsRepository(
                    calcsCosmosRepostory,
                    calculationProviderResultsSearchRepository,
                    specificationsRepository,
                    logger,
                    providerCalculationResultsSearchRepository,
                    featureToggle,
                    engineSettings);
            });

            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>((ctx) =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                config.Bind("CommonStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                return new SourceFileRepository(blobStorageOptions);
            });

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
                .AddSingleton<Services.Calculator.Interfaces.ICalculationsRepository, Services.Calculator.CalculationsRepository>();

            builder
               .AddSingleton<IDatasetAggregationsRepository, DatasetAggregationsRepository>();

            builder
                .AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();

            builder
                .AddSingleton<ISourceCodeService, SourceCodeService>();

            builder.AddCalcsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);

            builder.AddJobsInterServiceClient(config);

            builder.AddDatasetsInterServiceClient(config);

            builder.AddEngineSettings(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsights(config, "CalculateFunding.Functions.CalcEngine");

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.CalcEngine");

            builder.AddLogging("CalculateFunding.Functions.CalcEngine", config);

            builder.AddTelemetry();

            builder.AddSearch(config);

            builder.AddFeatureToggling(config);

            PolicySettings policySettings = builder.GetPolicySettings(config);
            CalculatorResiliencePolicies calcResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<ICalculatorResiliencePolicies>(calcResiliencePolicies);
            builder.AddSingleton<IJobHelperResiliencePolicies>(calcResiliencePolicies);

            builder.AddSingleton<IValidator<ICalculatorResiliencePolicies>, CalculatorResiliencePoliciesValidator>();

            return builder.BuildServiceProvider();
        }

        private static CalculatorResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            CalculatorResiliencePolicies resiliencePolicies = new CalculatorResiliencePolicies()
            {
                ProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                ProviderSourceDatasetsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                Messenger = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }
    }
}
