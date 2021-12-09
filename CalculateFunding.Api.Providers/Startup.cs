using System;
using AutoMapper;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.FundingDataZone;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Providers;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly.Bulkhead;
using CalculateFunding.Common.JobManagement;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Providers.Requests;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Providers.MappingProfiles;

namespace CalculateFunding.Api.Providers
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ServiceProvider { get; private set; }

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
            if (!env.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            if (Configuration.IsSwaggerEnabled())
            {
                app.ConfigureSwagger(title: "Provider Microservice API");
            }

            app.MapWhen(
                    context => !context.Request.Path.Value.StartsWith("/swagger"),
                    appBuilder => {
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

            builder.AddSingleton<IValidator<SetFundingStreamCurrentProviderVersionRequest>, SetFundingStreamCurrentProviderVersionRequestValidator>();
            builder.AddSingleton<IFundingStreamProviderVersionService, FundingStreamProviderVersionService>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddCaching(Configuration);

            builder
                .AddSingleton<IProviderVersionService, ProviderVersionService>()
                .AddSingleton<IProviderSnapshotPersistService, ProviderSnapshotPersistService>()
                .AddSingleton<IHealthChecker, ProviderVersionService>();

            builder
                .AddSingleton<IProviderVersionSearchService, ProviderVersionSearchService>()
                .AddSingleton<IHealthChecker, ProviderVersionSearchService>();

            builder
                .AddSingleton<IJobManagement, JobManagement>();

            builder
                .AddSingleton<IScopedProvidersService, ScopedProvidersService>()
                .AddSingleton<IHealthChecker, ScopedProvidersService>();

            builder
                .AddSingleton<IProviderVersionUpdateCheckService, ProviderVersionUpdateCheckService>()
                .AddSingleton<IPublishingJobClashCheck, PublishingJobClashCheck>();

            builder.AddSingleton<IValidator<ProviderVersionViewModel>, UploadProviderVersionValidator>();

            builder.AddSearch(this.Configuration);
            builder
              .AddSingleton<ISearchRepository<ProvidersIndex>, SearchRepository<ProvidersIndex>>();

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "providerversions";

                    return new BlobClient(storageSettings);
                });

            builder.AddSingleton<IProviderVersionsMetadataRepository, ProviderVersionsMetadataRepository>(
                ctx =>
                {
                    CosmosDbSettings specRepoDbSettings = new CosmosDbSettings();

                    Configuration.Bind("CosmosDbSettings", specRepoDbSettings);

                    specRepoDbSettings.ContainerName = "providerversionsmetadata";

                    CosmosRepository cosmosRepository = new CosmosRepository(specRepoDbSettings);

                    return new ProviderVersionsMetadataRepository(cosmosRepository);
                });

            builder.AddPolicySettings(Configuration);

            MapperConfiguration providerVersionsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<ProviderVersionsMappingProfile>();
            });

            builder
                .AddSingleton(providerVersionsConfig.CreateMapper());

            builder.AddPoliciesInterServiceClient(Configuration);
            builder.AddResultsInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddFundingDataServiceInterServiceClient(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Providers");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Providers");
            builder.AddLogging("CalculateFunding.Api.Providers");
            builder.AddTelemetry();

            builder.AddServiceBus(Configuration);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(Configuration);

            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);
            ProvidersResiliencePolicies resiliencePolicies = CreateResiliencePolicies(totalNetworkRequestsPolicy);

            builder.AddSingleton<IJobManagementResiliencePolicies>(resiliencePolicies);

            builder.AddSingleton<IProvidersResiliencePolicies>(resiliencePolicies);

            builder
                .AddSingleton<IFileSystemCache, FileSystemCache>()
                .AddSingleton<IFileSystemAccess, FileSystemAccess>()
                .AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();

            builder
                .AddSingleton<IProviderVersionServiceSettings>(ctx =>
                {
                    ProviderVersionServiceSettings settings = new ProviderVersionServiceSettings();

                    Configuration.Bind("providerversionservicesettings", settings);

                    return settings;
                });

            builder
               .AddSingleton<IScopedProvidersServiceSettings>(ctx =>
               {
                   ScopedProvidersServiceSettings settings = new ScopedProvidersServiceSettings();

                   Configuration.Bind("scopedprovidersservicesetting", settings);

                   return settings;
               });

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHealthCheckMiddleware();

            builder.AddHttpContextAccessor();           

            builder.AddSearch(Configuration);

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Provider Microservice API");
            }
        }

        private static ProvidersResiliencePolicies CreateResiliencePolicies(AsyncBulkheadPolicy totalNetworkRequestsPolicy)
        {
            return new ProvidersResiliencePolicies
            {
                ProviderVersionsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                ProviderVersionMetadataRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                BlobRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(),
                FundingDataZoneApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
