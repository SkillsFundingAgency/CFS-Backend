using System;
using AutoMapper;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Functions.CalcEngine.ServiceBus;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.Validators;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.MappingProfiles;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;

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
            builder.AddScoped<ICalculationEngineService, CalculationEngineService>();
            builder.AddSingleton<ICalculationEngine, CalculationEngine>();
            builder.AddSingleton<IAllocationFactory, AllocationFactory>();
            builder.AddScoped<IJobHelperService, JobHelperService>();
            builder.AddSingleton<IProviderSourceDatasetVersionKeyProvider, ProviderSourceDatasetVersionKeyProvider>();
            builder.AddSingleton<IFileSystemAccess, FileSystemAccess>();

            builder.AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>(ctx =>
            {
                FileSystemCacheSettings settings = new FileSystemCacheSettings();

                config.Bind("FileSystemCacheSettings", settings);

                return settings;
            });
            builder.AddSingleton<IFileSystemCache, FileSystemCache>();

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>((ctx) =>
            {
                CosmosDbSettings providerSourceDatasetsCosmosSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", providerSourceDatasetsCosmosSettings);

                providerSourceDatasetsCosmosSettings.ContainerName = "providerdatasets";

                CosmosRepository calcsCosmosRepository = new CosmosRepository(providerSourceDatasetsCosmosSettings);
                EngineSettings engineSettings = ctx.GetService<EngineSettings>();
                IFileSystemCache fileSystemCache = ctx.GetService<IFileSystemCache>();
                IProviderSourceDatasetVersionKeyProvider versionKeyProvider = ctx.GetService<IProviderSourceDatasetVersionKeyProvider>();

                return new ProviderSourceDatasetsRepository(calcsCosmosRepository, engineSettings, versionKeyProvider, fileSystemCache);
            });

            builder.AddSingleton<IProviderResultCalculationsHashProvider, ProviderResultCalculationsHashProvider>();

            builder.AddSingleton<IProviderResultsRepository, ProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings calcResultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcResultsDbSettings);

                calcResultsDbSettings.ContainerName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calcResultsDbSettings);

                ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository = ctx.GetService<ISearchRepository<ProviderCalculationResultsIndex>>();

                ISpecificationsApiClient specificationsApiClient = ctx.GetService<ISpecificationsApiClient>();

                ILogger logger = ctx.GetService<ILogger>();

                IFeatureToggle featureToggle = ctx.GetService<IFeatureToggle>();

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                IProviderResultCalculationsHashProvider calculationsHashProvider = ctx.GetService<IProviderResultCalculationsHashProvider>();

                ICalculatorResiliencePolicies calculatorResiliencePolicies = ctx.GetService<ICalculatorResiliencePolicies>();

                return new ProviderResultsRepository(
                    calcsCosmosRepostory,
                    specificationsApiClient,
                    logger,
                    providerCalculationResultsSearchRepository,
                    featureToggle,
                    engineSettings,
                    calculationsHashProvider,
                    calculatorResiliencePolicies);
            });

            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>((ctx) =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                config.Bind("AzureStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                return new SourceFileRepository(blobStorageOptions);
            });

            builder
                .AddSingleton<Services.CalcEngine.Interfaces.ICalculationsRepository, Services.CalcEngine.CalculationsRepository>();

            builder
               .AddSingleton<IDatasetAggregationsRepository, DatasetAggregationsRepository>();

            builder
                .AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();

            builder
                .AddSingleton<ISourceCodeService, SourceCodeService>();

            MapperConfiguration calculationsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();
            });

            builder
                .AddSingleton(calculationsConfig.CreateMapper());

           
            Common.Config.ApiClient.Calcs.ServiceCollectionExtensions.AddCalculationsInterServiceClient(builder, config);          
            Common.Config.ApiClient.Specifications.ServiceCollectionExtensions.AddSpecificationsInterServiceClient(builder, config);
          
            Common.Config.ApiClient.Jobs.ServiceCollectionExtensions.AddJobsInterServiceClient(builder, config);

            builder.AddDatasetsInterServiceClient(config);

            builder.AddEngineSettings(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsForFunctionApps(config, "CalculateFunding.Functions.CalcEngine");

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.CalcEngine");

            builder.AddLogging("CalculateFunding.Functions.CalcEngine", config);

            builder.AddTelemetry();

            builder.AddSearch(config);
            builder
               .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

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
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
            };

            return resiliencePolicies;
        }
    }
}
