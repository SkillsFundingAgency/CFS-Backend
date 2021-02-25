using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.FundingDataZone;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Functions.Providers.ServiceBus;
using CalculateFunding.Functions.Providers.Timer;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Providers;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.MappingProfiles;
using CalculateFunding.Services.Providers.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Providers.Startup))]

namespace CalculateFunding.Functions.Providers
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
                builder.AddScoped<OnPopulateScopedProvidersEventTrigger>();
                builder.AddScoped<OnPopulateScopedProvidersEventTriggerFailure>();
                builder.AddScoped<OnProviderSnapshotDataLoadEventTrigger>();
                builder.AddScoped<OnProviderSnapshotDataLoadEventTriggerFailure>();
                builder.AddScoped<OnNewProviderVersionCheck>();
            }

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton<IConfiguration>(config);

            builder
                .AddSingleton<IProviderVersionService, ProviderVersionService>()
                .AddSingleton<IHealthChecker, ProviderVersionService>();

            builder
                .AddSingleton<IProviderSnapshotDataLoadService, ProviderSnapshotDataLoadService>()
                .AddSingleton<IHealthChecker, ProviderSnapshotDataLoadService>();

            builder
                .AddSingleton<IScopedProvidersService, ScopedProvidersService>()
                .AddSingleton<IHealthChecker, ScopedProvidersService>();

            builder
                .AddSingleton<IProviderVersionUpdateCheckService, ProviderVersionUpdateCheckService>()
                .AddSingleton<IPublishingJobClashCheck, PublishingJobClashCheck>();

            builder
                .AddSingleton<IProvidersApiClient, ProvidersApiClient>();

            builder
                .AddSingleton<IJobManagement, JobManagement>();

            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddResultsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddFundingDataServiceInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddSingleton<IDeadletterService, DeadletterService>();

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Providers");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Providers");
            builder.AddLogging("CalculateFunding.Functions.Providers");
            builder.AddTelemetry();

            builder.AddFeatureToggling(config);

            MapperConfiguration providerVersionsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<ProviderVersionsMappingProfile>();
            });

            builder
                .AddSingleton(providerVersionsConfig.CreateMapper());

            builder
                .AddSingleton<IFileSystemCache, FileSystemCache>()
                .AddSingleton<IFileSystemAccess, FileSystemAccess>()
                .AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();

            builder.AddSearch(config);
            builder
              .AddSingleton<ISearchRepository<ProvidersIndex>, SearchRepository<ProvidersIndex>>();

            builder
                .AddSingleton<IProviderVersionServiceSettings>(ctx =>
                {
                    ProviderVersionServiceSettings settings = new ProviderVersionServiceSettings();

                    config.Bind("providerversionservicesettings", settings);

                    return settings;
                });

            builder
               .AddSingleton<IScopedProvidersServiceSettings>(ctx =>
               {
                   ScopedProvidersServiceSettings settings = new ScopedProvidersServiceSettings();

                   config.Bind("scopedprovidersservicesetting", settings);

                   return settings;
               });

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);
            ProvidersResiliencePolicies resiliencePolicies = CreateResiliencePolicies(totalNetworkRequestsPolicy);

            builder.AddSingleton<IJobManagementResiliencePolicies>(resiliencePolicies);

            builder.AddSingleton<IProvidersResiliencePolicies>(resiliencePolicies);

            builder.AddSingleton<IValidator<ProviderVersionViewModel>, UploadProviderVersionValidator>();

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "providerversions";

                    return new BlobClient(storageSettings);
                });

            builder.AddSingleton<IProviderVersionsMetadataRepository, ProviderVersionsMetadataRepository>(
                ctx =>
                {
                    CosmosDbSettings specRepoDbSettings = new CosmosDbSettings();

                    config.Bind("CosmosDbSettings", specRepoDbSettings);

                    specRepoDbSettings.ContainerName = "providerversionsmetadata";

                    CosmosRepository cosmosRepository = new CosmosRepository(specRepoDbSettings);

                    return new ProviderVersionsMetadataRepository(cosmosRepository);
                });

            builder.AddServiceBus(config, "providers");

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            return builder.BuildServiceProvider();
        }

        private static ProvidersResiliencePolicies CreateResiliencePolicies(AsyncBulkheadPolicy totalNetworkRequestsPolicy)
        {
            return new ProvidersResiliencePolicies
            {
                ProviderVersionsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ProviderVersionMetadataRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                BlobRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(),
                FundingDataZoneApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
