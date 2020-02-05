using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Polly.Bulkhead;

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
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            RegisterComponents(services);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Provider Microservice API", Version = "v1" });
                c.AddSecurityDefinition("API Key", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Ocp-Apim-Subscription-Key",
                    In = ParameterLocation.Header,
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseHealthCheckMiddleware();

            app.UseMvc();

            app.MapWhen(
                    context => !context.Request.Path.Value.StartsWith("/swagger"),
                    appBuilder => appBuilder.UseMiddleware<ApiKeyMiddleware>());

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Provider Microservice API");
                c.DocumentTitle = "Provider Microservice - Swagger";
            });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddCaching(Configuration);

            builder
                .AddSingleton<IProviderVersionService, ProviderVersionService>()
                .AddSingleton<IHealthChecker, ProviderVersionService>();

            builder
                .AddSingleton<IProviderVersionSearchService, ProviderVersionSearchService>()
                .AddSingleton<IHealthChecker, ProviderVersionSearchService>();

            builder
                .AddSingleton<IScopedProvidersService, ScopedProvidersService>()
                .AddSingleton<IHealthChecker, ScopedProvidersService>();

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

           
            Common.Config.ApiClient.Results.ServiceCollectionExtensions.AddResultsInterServiceClient(builder, Configuration);
            builder.AddApplicationInsightsTelemetry();            
            Common.Config.ApiClient.Specifications.ServiceCollectionExtensions.AddSpecificationsInterServiceClient(builder, Configuration);
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Providers");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Providers");
            builder.AddLogging("CalculateFunding.Api.Providers");
            builder.AddTelemetry();

            builder.AddSingleton<IProvidersResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ProvidersResiliencePolicies()
                {
                    ProviderVersionsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    ProviderVersionMetadataRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    BlobRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

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

            ServiceProvider = builder.BuildServiceProvider();

            builder.AddSearch(Configuration);


        }
    }
}
