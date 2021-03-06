﻿using AutoMapper;
using CacheCow.Server.Core.Mvc;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Graph;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specifications;
using CalculateFunding.Models.Specifications.ViewModels;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Specifications;
using CalculateFunding.Services.Specifications.Caching.Http;
using CalculateFunding.Services.Specifications.Interfaces;
using CalculateFunding.Services.Specifications.Validators;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.MappingProfiles;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Services.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly.Bulkhead;
using Serilog;
using BlobClient = CalculateFunding.Common.Storage.BlobClient;
using IBlobClient = CalculateFunding.Common.Storage.IBlobClient;
using LocalBlobClient = CalculateFunding.Services.Core.AzureStorage.BlobClient;
using LocalIBlobClient = CalculateFunding.Services.Core.Interfaces.AzureStorage.IBlobClient;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using SpecificationVersion = CalculateFunding.Models.Specs.SpecificationVersion;

namespace CalculateFunding.Api.Specs
{
    public class Startup
    {
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
                app.ConfigureSwagger(title: "Specs Microservice API");
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
            builder.AddHttpCachingMvc();

            builder.AddQueryProviderAndExtractorForViewModelMvc<
                FundingStructure,
                TemplateMetadataContentsTimedETagProvider,
                TemplateMatadataContentsTimedETagExtractor>(false);

            builder.AddSingleton<IFundingStructureService, FundingStructureService>()
                .AddSingleton<IValidator<UpdateFundingStructureLastModifiedRequest>, UpdateFundingStructureLastModifiedRequestValidator>();

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddSingleton<IQueueCreateSpecificationJobActions, QueueCreateSpecificationJobAction>();
            builder.AddSingleton<IQueueEditSpecificationJobActions, QueueEditSpecificationJobActions>();
            builder.AddSingleton<IQueueDeleteSpecificationJobActions, QueueDeleteSpecificationJobAction>();

            builder.AddSingleton<ISpecificationsRepository, SpecificationsRepository>((ctx) =>
            {
                CosmosDbSettings specsVersioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", specsVersioningDbSettings);

                specsVersioningDbSettings.ContainerName = "specs";

                CosmosRepository resultsRepostory = new CosmosRepository(specsVersioningDbSettings);

                return new SpecificationsRepository(resultsRepostory);
            });

            builder
                .AddSingleton<ISpecificationsService, SpecificationsService>()
                .AddSingleton<IHealthChecker, SpecificationsService>();

            builder.AddSingleton<ISpecificationIndexer, SpecificationIndexer>();
            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();
            builder.AddSingleton<ISpecificationIndexingService, SpecificationIndexingService>();

            builder
                .AddSingleton<IJobManagement, JobManagement>();

            builder.AddSingleton<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddSingleton<IValidator<SpecificationEditModel>, SpecificationEditModelValidator>();
            builder.AddSingleton<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddSingleton<IValidator<AssignSpecificationProviderVersionModel>, AssignSpecificationProviderVersionModelValidator>();
            builder
                .AddSingleton<ISpecificationsSearchService, SpecificationsSearchService>()
                .AddSingleton<IHealthChecker, SpecificationsSearchService>();
            builder.AddSingleton<IResultsRepository, ResultsRepository>();
            builder
                .AddSingleton<ISpecificationsReportService, SpecificationsReportService>()
                .AddSingleton<IHealthChecker, SpecificationsReportService>();

            builder.AddSingleton<ITemplateMetadataResolver>((ctx) =>
            {
                TemplateMetadataResolver resolver = new TemplateMetadataResolver();

                TemplateMetadataGenerator schema10Generator = new TemplateMetadataGenerator(ctx.GetService<ILogger>());

                resolver.Register("1.0", schema10Generator);

                return resolver;
            });

            builder
                .AddSingleton<LocalIBlobClient, LocalBlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "providerversions";

                    return new LocalBlobClient(storageSettings);
                });

            builder.AddSingleton<ISpecificationTemplateVersionChangedHandler, SpecificationTemplateVersionChangedHandler>();

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    BlobStorageOptions storageSettings = new BlobStorageOptions();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "providerversions";

                    IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                    return new BlobClient(blobContainerRepository);
                });

            builder.AddSingleton<IVersionRepository<Models.Specs.SpecificationVersion>, VersionRepository<Models.Specs.SpecificationVersion>>((ctx) =>
            {
                CosmosDbSettings specsVersioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", specsVersioningDbSettings);

                specsVersioningDbSettings.ContainerName = "specs";

                CosmosRepository resultsRepostory = new CosmosRepository(specsVersioningDbSettings);

                return new VersionRepository<Models.Specs.SpecificationVersion>(resultsRepostory, new NewVersionBuilderFactory<SpecificationVersion>());
            });

            MapperConfiguration mappingConfig = new MapperConfiguration(
                c =>
                {
                    c.AddProfile<SpecificationsMappingProfile>();
                }
            );

            builder.AddFeatureToggling(Configuration);

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddServiceBus(Configuration);

            builder.AddSearch(Configuration);
            builder
             .AddSingleton<ISearchRepository<SpecificationIndex>, SearchRepository<SpecificationIndex>>();

            builder.AddCaching(Configuration);

            builder.AddResultsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddGraphInterServiceClient(Configuration);
            builder.AddCalculationsInterServiceClient(Configuration);
            builder.AddProvidersInterServiceClient(Configuration);
            builder.AddPoliciesInterServiceClient(Configuration);
            builder.AddDatasetsInterServiceClient(Configuration);

            builder.AddPolicySettings(Configuration);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(Configuration);

            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            builder.AddSingleton<ISpecificationsResiliencePolicies>((ctx) =>
            {
                Polly.AsyncPolicy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new SpecificationsResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CalcsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    DatasetsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationsSearchRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

            });

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Apis.Specs");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Specs");
            builder.AddLogging("CalculateFunding.Apis.Specs");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Specs Microservice API");
            }
        }
    }
}
