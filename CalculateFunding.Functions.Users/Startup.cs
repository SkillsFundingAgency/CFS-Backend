using System;
using AutoMapper;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Functions.Users.ServiceBus;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Users;
using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Services.Users.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Repositories.Common.Search;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Users.Startup))]

namespace CalculateFunding.Functions.Users
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
            builder.AddSingleton<IConfiguration>(config);

            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnEditSpecificationEvent>();
                builder.AddScoped<OnReIndexUsersEvent>();
            }

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
              .AddSingleton<IUserService, UserService>()
              .AddSingleton<IHealthChecker, UserService>();

            builder
               .AddSingleton<IFundingStreamPermissionService, FundingStreamPermissionService>()
               .AddSingleton<IHealthChecker, FundingStreamPermissionService>();

            builder
              .AddSingleton<IUserIndexingService, UserIndexingService>();

            builder
               .AddSingleton<IJobManagement, JobManagement>();

            builder.AddSearch(config);

            builder
             .AddSingleton<ISearchRepository<UserIndex>, SearchRepository<UserIndex>>();

            builder.AddSingleton<IBlobClient>(ctx =>
            {
                BlobStorageOptions options = new BlobStorageOptions();

                config.Bind("AzureStorageSettings", options);

                options.ContainerName = "userreports";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(options);
                return new BlobClient(blobContainerRepository);
            });

            builder
                .AddSingleton<ICsvUtils, CsvUtils>()
                .AddSingleton<IFileSystemAccess, FileSystemAccess>()
                .AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();


            builder
                .AddSingleton<IUsersCsvTransformServiceLocator, FundingStreamPermissionsUsersCsvTransformServiceLocator>()
                .AddSingleton<IUsersCsvTransform, FundingStreamUserPermissionsCsvTransform>()
                .AddSingleton<IUsersCsvGenerator, FundingStreamPermissionsUsersCsvGenerator>();

            builder.AddSingleton<IValidator<UserCreateModel>, UserCreateModelValidator>();

            builder.AddSingleton<IUserRepository, UserRepository>((ctx) =>
            {
                CosmosDbSettings usersDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", usersDbSettings);

                usersDbSettings.ContainerName = "users";

                CosmosRepository usersCosmosRepostory = new CosmosRepository(usersDbSettings);

                return new UserRepository(usersCosmosRepostory);
            });

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<UsersMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddSingleton<IVersionRepository<FundingStreamPermissionVersion>, VersionRepository<FundingStreamPermissionVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.ContainerName = "users";

                CosmosRepository versioningRepository = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<FundingStreamPermissionVersion>(versioningRepository, new NewVersionBuilderFactory<FundingStreamPermissionVersion>());
            });

            builder.AddPolicySettings(config);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);

            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            builder.AddSingleton<IUsersResiliencePolicies>((ctx) =>
            {
                return new UsersResiliencePolicies
                {
                    FundingStreamPermissionVersionRepositoryPolicy = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    CacheProviderPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                    SpecificationApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    UserRepositoryPolicy = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    UsersSearchRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

            });

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Users");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Users");
            builder.AddLogging("CalculateFunding.Functions.Users");

            builder.AddServiceBus(config, "users");
            builder.AddTelemetry();

            builder.AddSpecificationsInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            return builder.BuildServiceProvider();
        }
    }
}
