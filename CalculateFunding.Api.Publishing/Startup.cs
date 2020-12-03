using System;
using AutoMapper;
using CacheCow.Server.Core.Mvc;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Profiling;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Services.Publishing.Caching.Http;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Helper;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.IoC;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.SqlExport;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Polly;
using Polly.Bulkhead;
using Serilog;
using BlobClient = CalculateFunding.Common.Storage.BlobClient;
using IBlobClient = CalculateFunding.Common.Storage.IBlobClient;
using LocalBlobClient = CalculateFunding.Services.Core.AzureStorage.BlobClient;
using LocalIBlobClient = CalculateFunding.Services.Core.Interfaces.AzureStorage.IBlobClient;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;
using TemplateMetadataSchema11 = CalculateFunding.Common.TemplateMetadata.Schema11;

namespace CalculateFunding.Api.Publishing
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

            services.AddFeatureManagement();
        }

        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            //TODO: this is required for dynamic changes and more config implementation is required
            //app.UseAzureAppConfiguration();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();

                app.UseMiddleware<LoggedInUserMiddleware>();
            }

            app.UseHttpsRedirection();

            if (Configuration.IsSwaggerEnabled())
            {
                app.ConfigureSwagger(title: "Publishing Microservice API");
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

        private void RegisterComponents(IServiceCollection builder)
        {
            builder.AddSingleton<IBatchUploadQueryService, BatchUploadQueryService>();
            builder.AddSingleton<IUniqueIdentifierProvider, UniqueIdentifierProvider>();
            builder.AddSingleton<IBatchUploadValidationService, BatchUploadValidationService>();
            builder.AddSingleton<IBatchUploadReaderFactory, BatchUploadReaderFactory>();
            builder.AddSingleton<IValidator<BatchUploadValidationRequest>, BatchUploadValidationRequestValidation>();
            
            builder.AddSingleton<IPublishedProviderUpdateDateService, PublishedProviderUpdateDateService>();
            
            ISqlSettings sqlSettings = new SqlSettings();

            Configuration.Bind("saSql", sqlSettings);

            builder.AddSingleton(sqlSettings);

            builder.AddSingleton<IBatchUploadService, BatchUploadService>(); 
                
            builder.AddScoped<IDataTableImporter, DataTableImporter>();
            builder.AddScoped<ISqlImportContextBuilder, SqlImportContextBuilder>();
            builder.AddSingleton<ISqlPolicyFactory, SqlPolicyFactory>();
            builder.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            builder.AddScoped<ISqlImportContextBuilder, SqlImportContextBuilder>();
            builder.AddScoped<ISqlImporter, SqlImporter>();
            builder.AddScoped<ISqlImportService, SqlImportService>();
            builder.AddScoped<ISqlNameGenerator, SqlNameGenerator>();
            builder.AddScoped<ISqlSchemaGenerator, SqlSchemaGenerator>();
            builder.AddScoped<IQaSchemaService, QaSchemaService>();
            builder.AddScoped<IQaRepository, QaRepository>();
            builder .AddSingleton<ITemplateMetadataResolver>(ctx =>
            {
                TemplateMetadataResolver resolver = new TemplateMetadataResolver();
                ILogger logger = ctx.GetService<ILogger>();
                    
                TemplateMetadataSchema10.TemplateMetadataGenerator schema10Generator = new TemplateMetadataSchema10.TemplateMetadataGenerator(logger);
                resolver.Register("1.0", schema10Generator);

                TemplateMetadataSchema11.TemplateMetadataGenerator schema11Generator = new TemplateMetadataSchema11.TemplateMetadataGenerator(logger);
                resolver.Register("1.1", schema11Generator);

                return resolver;
            });
            builder.AddSingleton<ICosmosRepository, CosmosRepository>();
            
            CosmosDbSettings settings = new CosmosDbSettings();

            Configuration.Bind("CosmosDbSettings", settings);

            settings.ContainerName = "publishedfunding";

            builder.AddSingleton(settings);

            builder.AddSingleton<IPublishedFundingContentsGeneratorResolver>(ctx =>
            {
                PublishedFundingContentsGeneratorResolver resolver = new PublishedFundingContentsGeneratorResolver();
                
                resolver.Register("1.0", new Generators.Schema10.PublishedFundingContentsGenerator());
                resolver.Register("1.1", new Generators.Schema11.PublishedFundingContentsGenerator());

                return resolver;
            });

            builder.AddSingleton<IPublishedFundingIdGeneratorResolver>(ctx =>
            {
                PublishedFundingIdGeneratorResolver resolver = new PublishedFundingIdGeneratorResolver();

                IPublishedFundingIdGenerator v10Generator = new Generators.Schema10.PublishedFundingIdGenerator();

                resolver.Register("1.0", v10Generator);
                resolver.Register("1.1", v10Generator);

                return resolver;
            });
            
            builder.AddSingleton<IProfilePatternPreview, ProfilePatternPreview>();
            builder.AddSingleton<IReProfilingRequestBuilder, ReProfilingRequestBuilder>();
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddSingleton<IProfileHistoryService, ProfileHistoryService>();
            builder.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder
                .AddSingleton<IPublishedProviderVersionService, PublishedProviderVersionService>()
                .AddSingleton<IHealthChecker, PublishedProviderVersionService>();
            builder.AddSingleton<ISpecificationFundingStatusService, SpecificationFundingStatusService>();
            builder
                .AddSingleton<IPublishedSearchService, PublishedSearchService>()
                .AddSingleton<IHealthChecker, PublishedSearchService>();

            builder.AddSingleton<IPoliciesService, PoliciesService>();
            builder.AddSingleton<IPublishedProviderStatusService>((ctx) => 
            {
                AzureStorageSettings storageSettings = new AzureStorageSettings();
                Configuration.Bind("AzureStorageSettings", storageSettings);
                storageSettings.ContainerName = "publishingconfirmation";

                return new PublishedProviderStatusService(
                    ctx.GetService<ISpecificationIdServiceRequestValidator>(),
                    ctx.GetService<ISpecificationService>(),
                    ctx.GetService<IPublishedFundingRepository>(),
                    ctx.GetService<IPublishingResiliencePolicies>(),
                    ctx.GetService<IPublishedProviderFundingCountProcessor>(),
                    ctx.GetService<IPublishedProviderFundingCsvDataProcessor>(),
                    ctx.GetService<ICsvUtils>(),
                    new LocalBlobClient(storageSettings));
            });
            builder.AddScoped<IProfileTotalsService, ProfileTotalsService>();
            builder.AddSingleton<IFundingConfigurationService, FundingConfigurationService>();

            builder.AddScoped<IFundingStreamPaymentDatesQuery, FundingStreamPaymentDatesQuery>();
            builder.AddScoped<IFundingStreamPaymentDatesIngestion, FundingStreamPaymentDatesIngestion>();
            builder.AddSingleton<ICsvUtils, CsvUtils>();
            builder.AddScoped<ICustomProfileService, CustomProfilingService>();
            builder.AddScoped<IValidator<ApplyCustomProfileRequest>, ApplyCustomProfileRequestValidator>();
            builder.AddSingleton<IPublishedProviderStatusUpdateService, PublishedProviderStatusUpdateService>();
            builder.AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>();
            builder.AddSingleton<IJobTracker, JobTracker>();
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<IVersionRepository<PublishedProviderVersion>, VersionRepository<PublishedProviderVersion>>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                CosmosRepository cosmos = new CosmosRepository(settings);

                return new VersionRepository<PublishedProviderVersion>(cosmos);
            });
            builder
                .AddSingleton<IPublishedProviderStatusUpdateSettings>(_ =>
                    {
                        PublishedProviderStatusUpdateSettings settings = new PublishedProviderStatusUpdateSettings();

                        Configuration.Bind("PublishedProviderStatusUpdateSettings", settings);

                        return settings;
                    }
                );
            builder.AddHttpClient(HttpClientKeys.Profiling,
                    c =>
                    {
                        ApiOptions apiOptions = new ApiOptions();

                        Configuration.Bind("providerProfilingClient", apiOptions);

                        Services.Core.Extensions.ServiceCollectionExtensions.SetDefaultApiClientConfigurationOptions(c, apiOptions, builder);
                    })
                .ConfigurePrimaryHttpMessageHandler(() => new ApiClientHandler())
                .AddTransientHttpErrorPolicy(c => c.WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) }))
                .AddTransientHttpErrorPolicy(c => c.CircuitBreakerAsync(100, TimeSpan.FromSeconds(30)));

            builder.AddSingleton<IFundingStreamPaymentDatesRepository>((ctx) =>
            {
                CosmosDbSettings cosmosSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", cosmosSettings);

                cosmosSettings.ContainerName = "profiling";

                return new FundingStreamPaymentDatesRepository(new CosmosRepository(cosmosSettings));
            });

            builder
                .AddSingleton<IPublishedFundingQueryBuilder, PublishedFundingQueryBuilder>();

            builder.AddSingleton<IPublishedFundingRepository, PublishedFundingRepository>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                CosmosRepository calcsCosmosRepository = new CosmosRepository(settings);
                IPublishedFundingQueryBuilder publishedFundingQueryBuilder = ctx.GetService<IPublishedFundingQueryBuilder>();

                return new PublishedFundingRepository(calcsCosmosRepository, publishedFundingQueryBuilder);
            });

            builder
                .AddSingleton<IPublishingEngineOptions>(_ => new PublishingEngineOptions(Configuration));
            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    BlobStorageOptions storageSettings = new BlobStorageOptions();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "publishedproviderversions";

                    IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                    return new BlobClient(blobContainerRepository);
                });

            builder.AddCaching(Configuration);
            builder.AddSearch(Configuration);
            builder
               .AddSingleton<ISearchRepository<PublishedProviderIndex>, SearchRepository<PublishedProviderIndex>>();
            builder
                .AddSingleton<ISearchRepository<PublishedFundingIndex>, SearchRepository<PublishedFundingIndex>>();

            builder
                .AddSingleton<IPublishedProviderProfilingService, PublishedProviderProfilingService>()
                .AddSingleton<IPublishedProviderErrorDetection, PublishedProviderErrorDetection>()
                .AddSingleton<IDetectPublishedProviderErrors, FundingLineValueProfileMismatchErrorDetector>()
                .AddSingleton<IProfilingService, ProfilingService>()
                .AddSingleton<IHealthChecker, ProfilingService>()
                .AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>();

            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Publishing");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Publishing");
            builder.AddLogging("CalculateFunding.Api.Publishing");
            builder.AddServiceBus(Configuration, "publishing");
            builder.AddTelemetry();
            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);
            builder.AddPolicySettings(Configuration);
            builder.AddHttpContextAccessor();           
            builder.AddHealthCheckMiddleware();
            
            builder.AddHttpCachingMvc();
            builder.AddQueryProviderAndExtractorForViewModelMvc<PublishedProviderFundingStructure, PublishedProviderFundingStructureTimedEtagProvider, PublishedProviderFundingStructureTimedEtagExtractor>(false);

            builder.AddPublishingServices(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddProvidersInterServiceClient(Configuration);
            builder.AddCalculationsInterServiceClient(Configuration);
            builder.AddProfilingInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddPoliciesInterServiceClient(Configuration);
            builder.AddFeatureToggling(Configuration);
            
            builder.AddScoped<IPublishedFundingUndoJobService, PublishedFundingUndoJobService>();
            builder.AddScoped<IPublishedFundingUndoJobCreation, PublishedFundingUndoJobCreation>();
            builder.AddScoped<IPublishedFundingUndoTaskFactoryLocator, PublishedFundingUndoTaskFactoryLocator>();
            builder.AddSingleton<IPublishedFundingUndoTaskFactory, SoftDeletePublishedFundingUndoTaskFactory>();
            builder.AddSingleton<IPublishedFundingUndoTaskFactory, HardDeletePublishedFundingUndoTaskFactory>();
            builder.AddSingleton<IPublishedFundingUndoCosmosRepository>(ctx =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                return new PublishedFundingUndoCosmosRepository(ctx.GetService<IPublishingResiliencePolicies>(),
                    new CosmosRepository(settings));
            });
            builder.AddSingleton<IPublishedFundingUndoBlobStoreRepository>(ctx =>
            {
                BlobStorageOptions settings = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", settings);

                settings.ContainerName = "publishedproviderversions";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(settings);
                return new PublishedFundingUndoBlobStoreRepository(new BlobClient(blobContainerRepository),
                    ctx.GetService<IPublishingResiliencePolicies>(),
                    ctx.GetService<ILogger>());
            });
            
            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();

            builder
                .AddSingleton<LocalIBlobClient, LocalBlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "publishedfunding";

                    return new LocalBlobClient(storageSettings);
                });

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            });

            builder.AddSingleton(resultsConfig.CreateMapper());

            builder.AddSingleton<IPublishingResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies
                {
                    SpecificationsRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedFundingRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    PublishedProviderVersionRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ProfilingApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedIndexSearchResiliencePolicy = PublishedIndexSearchResiliencePolicy.GeneratePublishedIndexSearch(),
                    PublishedProviderSearchRepository = PublishedIndexSearchResiliencePolicy.GeneratePublishedIndexSearch(),
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    FundingStreamPaymentDatesRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };

            });

            builder.AddSingleton<IPublishedProviderFundingCountProcessor, PublishedProviderFundingCountProcessor>();
            builder.AddSingleton<IPublishedProviderFundingCsvDataProcessor, PublishedProviderFundingCsvDataProcessor>();

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Publishing Microservice API");
            }
        }
    }
}