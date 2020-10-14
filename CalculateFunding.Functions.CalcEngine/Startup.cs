using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Functions.CalcEngine.ServiceBus;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.MappingProfiles;
using CalculateFunding.Services.CalcEngine.Validators;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.MappingProfiles;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DeadletterProcessor;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

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
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton<IConfiguration>(config);
            builder.AddCaching(config);

            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnCalcsGenerateAllocationResults>();
                builder.AddScoped<OnCalculationGenerateFailure>();
            }

            builder.AddScoped<ICalculationEngineService, CalculationEngineService>();
            builder.AddSingleton<ICalculationEngine, CalculationEngine>();
            builder.AddSingleton<IAllocationFactory, AllocationFactory>();
            builder.AddScoped<IJobHelperService, JobHelperService>();
            builder.AddScoped<IJobManagement, JobManagement>();
            builder.AddSingleton<IProviderSourceDatasetVersionKeyProvider, ProviderSourceDatasetVersionKeyProvider>();
            builder.AddSingleton<IFileSystemAccess, FileSystemAccess>();

            builder.AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();
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

                ILogger logger = ctx.GetService<ILogger>();

                IFeatureToggle featureToggle = ctx.GetService<IFeatureToggle>();

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                IProviderResultCalculationsHashProvider calculationsHashProvider = ctx.GetService<IProviderResultCalculationsHashProvider>();

                ICalculatorResiliencePolicies calculatorResiliencePolicies = ctx.GetService<ICalculatorResiliencePolicies>();

                IResultsApiClient resultsApiClient = ctx.GetService<IResultsApiClient>();

                IJobManagement jobManagement = ctx.GetService<IJobManagement>();

                return new ProviderResultsRepository(
                    calcsCosmosRepostory,
                    logger,
                    providerCalculationResultsSearchRepository,
                    featureToggle,
                    engineSettings,
                    calculationsHashProvider,
                    calculatorResiliencePolicies,
                    resultsApiClient,
                    jobManagement);
            });

            builder
                .AddSingleton<IBlobContainerRepository, BlobContainerRepository>();

            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>((ctx) =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                config.Bind("AzureStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                return new SourceFileRepository(blobStorageOptions, ctx.GetService<IBlobContainerRepository>());
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
                c.AddProfile<CalcEngineMappingProfile>();
            });

            builder
                .AddSingleton(calculationsConfig.CreateMapper());

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            builder.AddCalculationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddResultsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddDatasetsInterServiceClient(config);

            builder.AddEngineSettings(config);

            builder.AddServiceBus(config, "calcengine");

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.CalcEngine");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.CalcEngine");

            builder.AddLogging("CalculateFunding.Functions.CalcEngine", config);

            builder.AddTelemetry();

            builder.AddSearch(config);
            builder
               .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

            builder.AddFeatureToggling(config);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);
            CalculatorResiliencePolicies calcResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<ICalculatorResiliencePolicies>(calcResiliencePolicies);
            builder.AddSingleton<IJobHelperResiliencePolicies>(calcResiliencePolicies);
            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) => new JobManagementResiliencePolicies()
            {
                JobsApiClient = calcResiliencePolicies.JobsApiClient
            });

            builder.AddSingleton<IValidator<ICalculatorResiliencePolicies>, CalculatorResiliencePoliciesValidator>();
            builder.AddSingleton<ICalculationEngineServiceValidator, CalculationEngineServiceValidator>();
            builder.AddSingleton<ISpecificationAssemblyProvider, SpecificationAssemblyProvider>();
            builder.AddSingleton<IBlobContainerRepository, BlobContainerRepository>();
            builder.AddSingleton<IBlobClient>(ctx =>
            {
                BlobStorageOptions options = new BlobStorageOptions();
                
                config.Bind("AzureStorageSettings", options);
                
                options.ContainerName = "source";
                
                return new BlobClient(options, ctx.GetService<IBlobContainerRepository>());
            });
            

            return builder.BuildServiceProvider();
        }

        private static CalculatorResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            CalculatorResiliencePolicies resiliencePolicies = new CalculatorResiliencePolicies()
            {
                ProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                ProviderSourceDatasetsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                Messenger = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }
    }
}
