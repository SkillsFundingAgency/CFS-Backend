using AutoMapper;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Users;
using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Services.Users.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly.Bulkhead;
using System;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Repositories.Common.Search;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

namespace CalculateFunding.Api.Users
{
    public class Startup
    {
        private static readonly string AppConfigConnectionString = Environment.GetEnvironmentVariable("AzureConfiguration:ConnectionString");

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);

            services.AddControllers()
                .AddNewtonsoftJson();

            RegisterComponents(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!string.IsNullOrEmpty(AppConfigConnectionString))
            {
                app.UseAzureAppConfiguration();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            if (Configuration.IsSwaggerEnabled())
            {
                app.ConfigureSwagger(title: "Users Microservice API");
            }

            app.MapWhen(
                    context => !context.Request.Path.Value.StartsWith("/swagger"),
                    appBuilder =>
                    {
                        appBuilder.UseMiddleware<ApiKeyMiddleware>();
                        appBuilder.UseHealthCheckMiddleware();
                        appBuilder.UseMiddleware<LoggedInUserMiddleware>();
                        appBuilder.UseRouting();
                        appBuilder.UseAuthentication();
                        appBuilder.UseAuthorization();
                        appBuilder.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder.AddAppConfiguration();

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder
               .AddSingleton<IUserService, UserService>()
               .AddSingleton<IUserSearchService, UserSearchService>()
               .AddSingleton<IHealthChecker, UserService>();

            builder
               .AddSingleton<IFundingStreamPermissionService, FundingStreamPermissionService>()
               .AddSingleton<IHealthChecker, FundingStreamPermissionService>();

            builder
                .AddSingleton<IUserIndexingService, UserIndexingService>();

            builder
               .AddSingleton<IJobManagement, JobManagement>();

            builder.AddSearch(Configuration);

            builder
             .AddSingleton<ISearchRepository<UserIndex>, SearchRepository<UserIndex>>();

            builder
                .AddSingleton<ICsvUtils, CsvUtils>()
                .AddSingleton<IFileSystemAccess, FileSystemAccess>()
                .AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();

            builder.AddSingleton<IBlobClient>(ctx =>
            {
                BlobStorageOptions options = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", options);

                options.ContainerName = "userreports";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(options);
                return new BlobClient(blobContainerRepository);
            });

            builder
                .AddSingleton<IUsersCsvTransformServiceLocator, FundingStreamPermissionsUsersCsvTransformServiceLocator>()
                .AddSingleton<IUsersCsvTransform, FundingStreamUserPermissionsCsvTransform>()
                .AddSingleton<IUsersCsvGenerator, FundingStreamPermissionsUsersCsvGenerator>();


            builder.AddSingleton<IValidator<UserCreateModel>, UserCreateModelValidator>();

            builder.AddSingleton<IUserRepository, UserRepository>((ctx) =>
            {
                CosmosDbSettings usersDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", usersDbSettings);

                usersDbSettings.ContainerName = "users";

                CosmosRepository usersCosmosRepostory = new CosmosRepository(usersDbSettings);

                return new UserRepository(usersCosmosRepostory);
            });

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<UsersMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddSingleton<IVersionRepository<FundingStreamPermissionVersion>, VersionRepository<FundingStreamPermissionVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.ContainerName = "users";

                CosmosRepository versioningRepository = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<FundingStreamPermissionVersion>(versioningRepository, new NewVersionBuilderFactory<FundingStreamPermissionVersion>());
            });

            builder.AddPolicySettings(Configuration);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(Configuration);

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

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Users");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Users");
            builder.AddLogging("CalculateFunding.Api.Users");
            builder.AddTelemetry();

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();

            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Users Microservice API");
            }
        }

    }
}
