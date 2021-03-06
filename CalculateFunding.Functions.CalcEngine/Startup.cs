﻿using System;
using System.Net;
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
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Processing.Interfaces;
using FluentValidation;
using Microsoft.Azure.Cosmos;
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
            builder.AddScoped<ICalculationEngine, CalculationEngine>();
            builder.AddScoped<IAllocationFactory, AllocationFactory>();
            builder.AddScoped<IDeadletterService, DeadletterService>();
            builder.AddScoped<IJobManagement, JobManagement>();
            builder.AddSingleton<IProviderSourceDatasetVersionKeyProvider, ProviderSourceDatasetVersionKeyProvider>();
            builder.AddSingleton<IFileSystemAccess, FileSystemAccess>();

            builder.AddSingleton<IAssemblyService, AssemblyService>();
            builder.AddSingleton<ICalculationAggregationService, CalculationAggregationService>();
            builder.AddScoped<ICalculationEnginePreviewService, CalculationEnginePreviewService>();

            builder.AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();
            builder.AddSingleton<IFileSystemCache, FileSystemCache>();

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>((ctx) =>
            {
                CosmosDbSettings providerSourceDatasetsCosmosSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", providerSourceDatasetsCosmosSettings);

                providerSourceDatasetsCosmosSettings.ContainerName = "providerdatasets";

                CosmosRepository calcsCosmosRepository = new CosmosRepository(providerSourceDatasetsCosmosSettings, new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = 8,
                    MaxTcpConnectionsPerEndpoint = 4,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true,
                    // MaxRetryAttemptsOnRateLimitedRequests = 1,
                    // MaxRetryWaitTimeOnRateLimitedRequests = new TimeSpan(0, 0, 30),
                });

                ICalculatorResiliencePolicies calculatorResiliencePolicies = ctx.GetService<ICalculatorResiliencePolicies>();

                return new ProviderSourceDatasetsRepository(calcsCosmosRepository, calculatorResiliencePolicies);
            });

            builder.AddSingleton<IProviderResultCalculationsHashProvider, ProviderResultCalculationsHashProvider>();

            builder.AddSingleton<IProviderResultsRepository, ProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings calcResultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcResultsDbSettings);

                calcResultsDbSettings.ContainerName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calcResultsDbSettings, new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = 8,
                    MaxTcpConnectionsPerEndpoint = 2,
                    // MaxRetryWaitTimeOnRateLimitedRequests = new TimeSpan(0, 0, 30),
                    AllowBulkExecution = true,
                });

                ILogger logger = ctx.GetService<ILogger>();

                IProviderResultCalculationsHashProvider calculationsHashProvider = ctx.GetService<IProviderResultCalculationsHashProvider>();

                ICalculatorResiliencePolicies calculatorResiliencePolicies = ctx.GetService<ICalculatorResiliencePolicies>();

                IResultsApiClient resultsApiClient = ctx.GetService<IResultsApiClient>();

                IJobManagement jobManagement = ctx.GetService<IJobManagement>();

                return new ProviderResultsRepository(
                    calcsCosmosRepostory,
                    logger,
                    calculationsHashProvider,
                    calculatorResiliencePolicies,
                    resultsApiClient,
                    jobManagement);
            });

            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>((ctx) =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                config.Bind("AzureStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(blobStorageOptions);
                return new SourceFileRepository(blobContainerRepository);
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
            builder.AddDatasetsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

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
            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) => new JobManagementResiliencePolicies()
            {
                JobsApiClient = calcResiliencePolicies.JobsApiClient
            });

            builder.AddSingleton<IValidator<ICalculatorResiliencePolicies>, CalculatorResiliencePoliciesValidator>();
            builder.AddSingleton<ICalculationEngineServiceValidator, CalculationEngineServiceValidator>();
            builder.AddSingleton<ISpecificationAssemblyProvider, SpecificationAssemblyProvider>();
            builder.AddSingleton<IBlobClient>(ctx =>
            {
                BlobStorageOptions options = new BlobStorageOptions();

                config.Bind("AzureStorageSettings", options);

                options.ContainerName = "source";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(options);
                return new BlobClient(blobContainerRepository);
            });

            ServicePointManager.DefaultConnectionLimit = 200;

            return builder.BuildServiceProvider();
        }

        private static CalculatorResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            CalculatorResiliencePolicies resiliencePolicies = new CalculatorResiliencePolicies()
            {
                CalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(),
                ProviderSourceDatasetsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                Messenger = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
            };

            return resiliencePolicies;
        }
    }
}
