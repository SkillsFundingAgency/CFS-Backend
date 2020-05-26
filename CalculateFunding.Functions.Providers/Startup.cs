using System;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Functions.Providers.ServiceBus;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Providers;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.AzureStorage;
using FluentValidation;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Validators;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Models.Providers;
using System.Threading;
using CalculateFunding.Services.DeadletterProcessor;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Services;


[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Providers.Startup))]

namespace CalculateFunding.Functions.Providers
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
            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnPopulateScopedProvidersEventTrigger>();
                builder.AddScoped<OnPopulateScopedProvidersEventTriggerFailure>();
            }

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton<IConfiguration>(config);

            builder
                .AddSingleton<IProviderVersionService, ProviderVersionService>()
                .AddSingleton<IHealthChecker, ProviderVersionService>();

            builder
                .AddSingleton<IScopedProvidersService, ScopedProvidersService>()
                .AddSingleton<IHealthChecker, ScopedProvidersService>();

            builder
                .AddSingleton<IProvidersApiClient, ProvidersApiClient>();

            builder
                .AddSingleton<IJobManagement, JobManagement>();

            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddResultsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddSingleton<IJobHelperService, JobHelperService>();

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

            builder.AddSingleton<IJobHelperResiliencePolicies>(resiliencePolicies);

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
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
