﻿using System;
using System.Threading;
using AutoMapper;
using CacheCow.Server.Core.Mvc;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Graph;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Services.SqlExport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly.Bulkhead;
using Serilog;
using CommonStorage = CalculateFunding.Common.Storage;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;
using TemplateMetadataSchema11 = CalculateFunding.Common.TemplateMetadata.Schema11;
using TemplateMetadataSchema12 = CalculateFunding.Common.TemplateMetadata.Schema12;

namespace CalculateFunding.Api.Results
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
                app.ConfigureSwagger(title: "Results Microservice API");
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

        private void RegisterComponents(IServiceCollection builder)
        {
            builder.AddAppConfiguration();

            builder.AddSpecificationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan)
                .AddCalculationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddScoped<ISpecificationsWithProviderResultsService, SpecificationsWithProviderResultsService>();
            builder.AddScoped<ICalculationResultQADatabasePopulationService, CalculationResultQADatabasePopulationService>();
            builder.AddScoped<IProducerConsumerFactory, ProducerConsumerFactory>();

            builder.AddScoped<ISqlNameGenerator, SqlNameGenerator>();
            builder.AddScoped<ISqlSchemaGenerator, SqlSchemaGenerator>();
            builder.AddScoped<IQaSchemaService, QaSchemaService>();

            builder.AddScoped<IDataTableImporter, DataTableImporter>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                Configuration.Bind("crSql", sqlSettings);

                SqlConnectionFactory sqlConnectionFactory = new SqlConnectionFactory(sqlSettings);

                return new DataTableImporter(sqlConnectionFactory);
            });

            builder.AddScoped<IQaRepository, QaRepository>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                Configuration.Bind("crSql", sqlSettings);

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

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder
                .AddSingleton<IResultsService, ResultsService>()
                .AddSingleton<IHealthChecker, ResultsService>();

            builder
                .AddSingleton<IJobManagement, JobManagement>();

            builder
                .AddSingleton<IProviderCalculationResultsSearchService, ProviderCalculationResultsSearchService>()
                .AddSingleton<IHealthChecker, ProviderCalculationResultsSearchService>();

            builder.AddHttpCachingMvc();

            string key = Configuration.GetValue<string>("specificationsClient:ApiKey");

            builder.AddGraphInterServiceClient(Configuration);

            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
                           {
                               CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                               Configuration.Bind("CosmosDbSettings", calssDbSettings);

                               calssDbSettings.ContainerName = "calculationresults";

                               CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                               EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                               return new CalculationResultsRepository(calcsCosmosRepostory, engineSettings);
                           });

            builder.AddSingleton<IProviderSourceDatasetRepository, ProviderSourceDatasetRepository>((ctx) =>
            {
                CosmosDbSettings provDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", provDbSettings);

                provDbSettings.ContainerName = "providerdatasets";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(provDbSettings);

                return new ProviderSourceDatasetRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IProviderCalculationResultsReIndexerService, ProviderCalculationResultsReIndexerService>();

            builder
               .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "calcresultexports";

                    return new BlobClient(storageSettings);
                });

            builder.AddSingleton<CommonStorage.IBlobClient>(ctx =>
            {
                CommonStorage.BlobStorageOptions options = new CommonStorage.BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", options);

                options.ContainerName = "calcresultexports";

                CommonStorage.IBlobContainerRepository blobContainerRepository = new CommonStorage.BlobContainerRepository(options);
                return new CommonStorage.BlobClient(blobContainerRepository);
            });


            builder.AddSearch(Configuration);
            builder
                .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Results");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Results");
            builder.AddLogging("CalculateFunding.Api.Results");
            builder.AddTelemetry();

            builder.AddEngineSettings(Configuration);

            builder.AddJobsInterServiceClient(Configuration);
            builder.AddPoliciesInterServiceClient(Configuration);

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<ResultsMappingProfile>();
            });

            builder.AddSingleton(resultsConfig.CreateMapper());

            builder.AddPolicySettings(Configuration);

            builder.AddHttpContextAccessor();

            builder.AddFeatureToggling(Configuration);

            builder.AddSingleton<IResultsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies()
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
                    ProviderChangesRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ProviderCalculationResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                    BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHealthCheckMiddleware();

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Results Microservice API");
            }

        }
    }
}
