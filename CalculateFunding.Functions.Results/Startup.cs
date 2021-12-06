using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Functions.Results.Timer;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Services.Results.Repositories;
using CalculateFunding.Services.Results.SearchIndex;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using AzureStorage = CalculateFunding.Services.Core.AzureStorage;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using CommonStorage = CalculateFunding.Common.Storage;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Common.Sql;
using CalculateFunding.Common.TemplateMetadata;
using Serilog;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;
using TemplateMetadataSchema11 = CalculateFunding.Common.TemplateMetadata.Schema11;
using TemplateMetadataSchema12 = CalculateFunding.Common.TemplateMetadata.Schema12;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Results.Startup))]

namespace CalculateFunding.Functions.Results
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services, builder.GetFunctionsConfigurationToIncludeHostJson());
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfiguration azureFuncConfig = null)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig(azureFuncConfig);

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddAppConfiguration();

            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnProviderResultsSpecificationCleanup>();
                builder.AddScoped<OnReIndexCalculationResults>();
                builder.AddScoped<OnCalculationResultsCsvGeneration>();
                builder.AddScoped<OnCalculationResultsCsvGenerationTimer>();
                builder.AddScoped<OnMergeSpecificationInformationForProviderWithResults>();
                builder.AddScoped<OnMergeSpecificationInformationForProviderWithResultsFailure>();
                builder.AddScoped<OnPopulateCalculationResultsQADatabase>();
                builder.AddScoped<OnPopulateCalculationResultsQADatabaseFailure>();
                builder.AddScoped<OnDeleteCalculationResults>();
                builder.AddScoped<OnDeleteCalculationResultsFailure>();
                builder.AddScoped<OnSearchIndexWriterEventTrigger>();
                builder.AddScoped<OnSearchIndexWriterEventTriggerFailure>();
            }
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();
            builder.AddSingleton<ISpecificationsWithProviderResultsService, SpecificationsWithProviderResultsService>();
            builder.AddSingleton<ICalculationResultQADatabasePopulationService, CalculationResultQADatabasePopulationService>();
            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();

            builder.AddSingleton<IConfiguration>(config);
            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder.AddSingleton<IResultsService, ResultsService>();
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>();
            builder.AddSingleton<IDeadletterService, DeadletterService>();
            builder.AddSingleton<IProviderCalculationResultsReIndexerService, ProviderCalculationResultsReIndexerService>();
            builder.AddTransient<ICsvUtils, CsvUtils>();
            builder
                .AddTransient<IProviderResultsCsvGeneratorService, ProviderResultsCsvGeneratorService>()
                .AddTransient<IHealthChecker, ProviderResultsCsvGeneratorService>();

            builder.AddTransient<IProviderResultsToCsvRowsTransformation, ProviderResultsToCsvRowsTransformation>();
            builder.AddSingleton<IFileSystemAccess, FileSystemAccess>();
            builder.AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();

            builder
                 .AddSingleton<ISearchIndexWriterSettings, SearchIndexWriterSettings>((ctx) =>
                 {
                     IConfigurationSection setttingConfig = config.GetSection("searchIndexWriterSettings");
                     return new SearchIndexWriterSettings(setttingConfig);
                 });

            builder.AddSingleton<CommonStorage.IBlobClient>(ctx =>
            {
                CommonStorage.BlobStorageOptions options = new CommonStorage.BlobStorageOptions();

                config.Bind("AzureStorageSettings", options);

                options.ContainerName = "calcresults";

                CommonStorage.IBlobContainerRepository blobContainerRepository = new CommonStorage.BlobContainerRepository(options);
                return new CommonStorage.BlobClient(blobContainerRepository);
            });

            builder.AddScoped<ISearchIndexProcessorFactory, SearchIndexProcessorFactory>();
            builder.AddScoped<ISearchIndexDataReader<ProviderResultDataKey, ProviderResult>, ProviderCalculationResultsIndexDataReader>();
            builder.AddScoped<ISearchIndexTrasformer<ProviderResult, ProviderCalculationResultsIndex>, ProviderCalculationResultsIndexTransformer>();
            builder.AddScoped<ISearchIndexProcessor, ProviderCalculationResultsIndexProcessor>();
            builder.AddScoped<ISearchIndexWriterService, SearchIndexWriterService>();

            builder.AddScoped<ISqlNameGenerator, SqlNameGenerator>();
            builder.AddScoped<ISqlSchemaGenerator, SqlSchemaGenerator>();
            builder.AddScoped<IQaSchemaService, QaSchemaService>();

            builder.AddScoped<IDataTableImporter, DataTableImporter>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                config.Bind("crSql", sqlSettings);

                SqlConnectionFactory sqlConnectionFactory = new SqlConnectionFactory(sqlSettings);

                return new DataTableImporter(sqlConnectionFactory);
            });

            builder.AddScoped<IQaRepository, QaRepository>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                config.Bind("crSql", sqlSettings);

                SqlConnectionFactory sqlConnectionFactory = new SqlConnectionFactory(sqlSettings);
                SqlPolicyFactory sqlPolicyFactory = new SqlPolicyFactory();

                return new QaRepository(sqlConnectionFactory, sqlPolicyFactory);
            });

            builder.AddSingleton<ITemplateMetadataResolver>(ctx =>
            {
                TemplateMetadataResolver resolver = new TemplateMetadataResolver();
                ILogger logger = ctx.GetService<ILogger>();

                TemplateMetadataSchema10.TemplateMetadataGenerator schema10Generator = new TemplateMetadataSchema10.TemplateMetadataGenerator(logger);
                resolver.Register("1.0", schema10Generator);

                TemplateMetadataSchema11.TemplateMetadataGenerator schema11Generator = new TemplateMetadataSchema11.TemplateMetadataGenerator(logger);
                resolver.Register("1.1", schema11Generator);

                TemplateMetadataSchema12.TemplateMetadataGenerator schema12Generator = new TemplateMetadataSchema12.TemplateMetadataGenerator(logger);
                resolver.Register("1.2", schema12Generator);

                return resolver;
            });

            builder.AddCaching(config);

            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.ContainerName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                return new CalculationResultsRepository(calcsCosmosRepostory, engineSettings);
            });

            builder.AddSingleton<IProviderSourceDatasetRepository, ProviderSourceDatasetRepository>((ctx) =>
            {
                CosmosDbSettings provDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", provDbSettings);

                provDbSettings.ContainerName = "providerdatasets";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(provDbSettings);

                return new ProviderSourceDatasetRepository(calcsCosmosRepostory);
            });

            builder
                .AddSingleton<IBlobClient, AzureStorage.BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "calcresults";

                    return new AzureStorage.BlobClient(storageSettings);
                });

            builder.AddSearch(config);
            builder
               .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

            builder.AddServiceBus(config, "results");

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Results");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Results");
            builder.AddLogging("CalculateFunding.Functions.Results");
            builder.AddTelemetry();
            builder.AddEngineSettings(config);

            builder.AddCalculationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddProvidersInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<ResultsMappingProfile>();
            });

            builder.AddSingleton(resultsConfig.CreateMapper());

            builder.AddFeatureToggling(config);

            builder.AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);
            ResiliencePolicies resultsResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IResultsResiliencePolicies>(resultsResiliencePolicies);

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = new ResiliencePolicies()
            {
                CalculationProviderResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                ResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProviderProfilingRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderCalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                PublishedProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProviderCalculationResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ProviderChangesRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }
    }
}
