using AutoMapper;
using CacheCow.Server.Core.Mvc;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Config.ApiClient.FundingDataZone;
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
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Services.Publishing.Caching.Http;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.Helper;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.IoC;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.SqlExport;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Polly;
using Polly.Bulkhead;
using Serilog;
using System;
using BlobClient = CalculateFunding.Common.Storage.BlobClient;
using IBlobClient = CalculateFunding.Common.Storage.IBlobClient;
using LocalBlobClient = CalculateFunding.Services.Core.AzureStorage.BlobClient;
using LocalIBlobClient = CalculateFunding.Services.Core.Interfaces.AzureStorage.IBlobClient;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;
using TemplateMetadataSchema11 = CalculateFunding.Common.TemplateMetadata.Schema11;
using TemplateMetadataSchema12 = CalculateFunding.Common.TemplateMetadata.Schema12;

namespace CalculateFunding.Api.Publishing
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

            services.AddFeatureManagement();
        }

        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env)
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

                app.UseMiddleware<LoggedInUserMiddleware>();
            }

            app.UseHttpsRedirection();

            if (Configuration.IsSwaggerEnabled())
            {
                app.ConfigureSwagger(title: "Publishing Microservice API");
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

            builder.AddSingleton<IFundingLineRoundingSettings, FundingLineRoundingSettings>();
            builder.AddSingleton<IBatchProfilingOptions, BatchProfilingOptions>();
            builder.AddSingleton<IBatchProfilingService, BatchProfilingService>();
            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();

            builder.AddSingleton<IReProfilingResponseMapper, ReProfilingResponseMapper>();

            builder.AddSingleton<IBatchUploadQueryService, BatchUploadQueryService>();
            builder.AddSingleton<IUniqueIdentifierProvider, UniqueIdentifierProvider>();
            builder.AddSingleton<IBatchUploadValidationService, BatchUploadValidationService>();
            builder.AddSingleton<IBatchUploadReaderFactory, BatchUploadReaderFactory>();
            builder.AddSingleton<IValidator<BatchUploadValidationRequest>, BatchUploadValidationRequestValidation>();
            builder.AddSingleton<IValidator<ChannelRequest>, ChannelModelValidator>();

            builder.AddSingleton<IPublishedProviderUpdateDateService, PublishedProviderUpdateDateService>();

            builder.AddSingleton<IBatchUploadService, BatchUploadService>();


            builder.AddScoped<ISqlImportContextBuilder, SqlImportContextBuilder>();

            builder.AddScoped<ISqlImporter, SqlImporter>();
            builder.AddScoped<ISqlImportService, SqlImportService>();
            builder.AddScoped<ISqlNameGenerator, SqlNameGenerator>();
            builder.AddScoped<ISqlSchemaGenerator, SqlSchemaGenerator>();
            builder.AddScoped<IQaSchemaService, QaSchemaService>();

            builder.AddScoped<IDataTableImporter, DataTableImporter>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                Configuration.Bind("saSql", sqlSettings);

                SqlConnectionFactory sqlConnectionFactory = new SqlConnectionFactory(sqlSettings);

                return new DataTableImporter(sqlConnectionFactory);
            });

            builder.AddScoped<IQaRepository, QaRepository>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                Configuration.Bind("saSql", sqlSettings);

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
                resolver.Register("1.2", new Generators.Schema12.PublishedFundingContentsGenerator());

                return resolver;
            });

            builder.AddSingleton<IPublishedFundingIdGeneratorResolver>(ctx =>
            {
                PublishedFundingIdGeneratorResolver resolver = new PublishedFundingIdGeneratorResolver();

                IPublishedFundingIdGenerator v10Generator = new Generators.Schema10.PublishedFundingIdGenerator();

                resolver.Register("1.0", v10Generator);
                resolver.Register("1.1", v10Generator);
                resolver.Register("1.2", v10Generator);

                return resolver;
            });

            builder.AddScoped<IProfilePatternPreview, ProfilePatternPreview>();
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
            builder.AddSingleton<IPublishedProviderStatusService, PublishedProviderStatusService>();
            builder.AddScoped<IProfileTotalsService, ProfileTotalsService>();
            builder.AddSingleton<IFundingConfigurationService, FundingConfigurationService>();
            builder.AddScoped<IPublishService, PublishService>();

            builder.AddScoped<IFundingStreamPaymentDatesQuery, FundingStreamPaymentDatesQuery>();
            builder.AddScoped<IFundingStreamPaymentDatesIngestion, FundingStreamPaymentDatesIngestion>();
            builder.AddSingleton<ICsvUtils, CsvUtils>();
            builder.AddScoped<ICustomProfileService, CustomProfilingService>();
            builder.AddScoped<IValidator<ApplyCustomProfileRequest>, ApplyCustomProfileRequestValidator>();
            builder.AddSingleton<IPublishedProviderStatusUpdateService, PublishedProviderStatusUpdateService>();
            builder.AddScoped<IRefreshStateService, RefreshStateService>();
            builder.AddScoped<IOrganisationGroupService, OrganisationGroupService>();
            builder.AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>();
            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreationLocator, GeneratePublishedFundingCsvJobsCreationLocator>();
            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreation, GenerateRefreshPublishedFundingCsvJobsCreation>();
            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreation, GenerateApprovePublishedFundingCsvJobsCreation>();
            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreation, GenerateReleasePublishedFundingCsvJobsCreation>();
            builder.AddScoped<ICreateGeneratePublishedProviderEstateCsvJobs, CreateGeneratePublishedProviderEstateCsvJobs>();
            builder.AddTransient<ICreateGeneratePublishedFundingCsvJobs, GeneratePublishedFundingCsvJobCreation>();
            builder.AddTransient<ICreatePublishDatasetsDataCopyJob, PublishingDatasetsDataCopyJobCreation>();
            builder.AddTransient<ICreateProcessDatasetObsoleteItemsJob, ProcessDatasetObsoleteItemsJobCreation>();

            builder.AddScoped<IPublishedFundingCsvJobsService, PublishedFundingCsvJobsService>();
            builder.AddSingleton<IJobTracker, JobTracker>();
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<IVersionRepository<PublishedProviderVersion>, VersionRepository<PublishedProviderVersion>>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                CosmosRepository cosmos = new CosmosRepository(settings);

                return new VersionRepository<PublishedProviderVersion>(cosmos, new NewVersionBuilderFactory<PublishedProviderVersion>());
            });
            builder.AddSingleton<IVersionRepository<PublishedFundingVersion>, VersionRepository<PublishedFundingVersion>>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                CosmosRepository cosmos = new CosmosRepository(settings);

                return new VersionRepository<PublishedFundingVersion>(cosmos, new NewVersionBuilderFactory<PublishedFundingVersion>());
            });
            builder.AddSingleton<IVersionBulkRepository<PublishedProviderVersion>, VersionBulkRepository<PublishedProviderVersion>>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                CosmosRepository cosmos = new CosmosRepository(settings);

                return new VersionBulkRepository<PublishedProviderVersion>(cosmos, new NewVersionBuilderFactory<PublishedProviderVersion>());
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

            builder.AddSingleton<IPublishedFundingBulkRepository, PublishedFundingBulkRepository>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                IPublishingEngineOptions publishingEngineOptions = ctx.GetService<IPublishingEngineOptions>();

                CosmosRepository calcsCosmosRepository = new CosmosRepository(settings, publishingEngineOptions.AllowBatching ? new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = publishingEngineOptions.MaxRequestsPerTcpConnectionPublishedFundingCosmosBulkOptions,
                    MaxTcpConnectionsPerEndpoint = 4,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true
                } : null);

                IPublishingResiliencePolicies publishingResiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();

                return new PublishedFundingBulkRepository(publishingResiliencePolicies, publishingEngineOptions, calcsCosmosRepository);
            });

            builder.AddSingleton<IVersionBulkRepository<PublishedFundingVersion>, VersionBulkRepository<PublishedFundingVersion>>((ctx) =>
            {
                CosmosDbSettings PublishedFundingVersioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", PublishedFundingVersioningDbSettings);

                PublishedFundingVersioningDbSettings.ContainerName = "publishedfunding";

                IPublishingEngineOptions publishingEngineOptions = ctx.GetService<IPublishingEngineOptions>();

                CosmosRepository cosmosRepository = new CosmosRepository(PublishedFundingVersioningDbSettings, publishingEngineOptions.AllowBatching ? new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = publishingEngineOptions.MaxRequestsPerTcpConnectionPublishedFundingCosmosBulkOptions,
                    MaxTcpConnectionsPerEndpoint = publishingEngineOptions.MaxTcpConnectionsPerEndpointPublishedFundingCosmosBulkOptions,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true
                } : null);

                return new VersionBulkRepository<PublishedFundingVersion>(cosmosRepository, new NewVersionBuilderFactory<PublishedFundingVersion>());
            });

            builder.AddSingleton<IPublishedProviderContentsGeneratorResolver>(ctx =>
            {
                PublishedProviderContentsGeneratorResolver resolver = new PublishedProviderContentsGeneratorResolver();

                resolver.Register("1.0", new Generators.Schema10.PublishedProviderContentsGenerator());
                resolver.Register("1.1", new Generators.Schema11.PublishedProviderContentsGenerator());
                resolver.Register("1.2", new Generators.Schema12.PublishedProviderContentsGenerator());

                return resolver;
            });

            builder.AddSingleton<ITransactionFactory, TransactionFactory>();
            builder.AddSingleton<IPublishedFundingService, PublishedFundingService>();

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(Configuration);
            builder.AddSingleton<ITransactionResiliencePolicies>((ctx) => new TransactionResiliencePolicies()
            {
                TransactionPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings)
            });

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
                .AddSingleton<IErrorDetectionStrategyLocator, ErrorDetectionStrategyLocator>()
                .AddSingleton<IDetectPublishedProviderErrors, FundingLineValueProfileMismatchErrorDetector>()
                .AddSingleton<IProfilingService, ProfilingService>()
                .AddSingleton<IHealthChecker, ProfilingService>()
                .AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>();

            builder.AddSingleton<IAvailableFundingLinePeriodsService, AvailableFundingLinePeriodsService>();

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Publishing");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Publishing");
            builder.AddLogging("CalculateFunding.Api.Publishing");
            builder.AddTelemetry();

            builder.AddServiceBus(Configuration, "publishing");

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
            builder.AddFundingDataServiceInterServiceClient(Configuration);
            builder.AddDatasetsInterServiceClient(Configuration);

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
            builder.AddSingleton<IProviderFilter, ProviderFilter>();

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
                    PublishedFundingRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(),
                    PublishedProviderVersionRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ProfilingApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedIndexSearchResiliencePolicy = PublishedIndexSearchResiliencePolicy.GeneratePublishedIndexSearch(),
                    PublishedProviderSearchRepository = PublishedIndexSearchResiliencePolicy.GeneratePublishedIndexSearch(),
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    FundingStreamPaymentDatesRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    DatasetsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedFundingBlobRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
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

            builder.AddSingleton<IPublishingV3ToSqlMigrator, PublishingV3ToSqlMigrator>();
            builder.AddSingleton<IPublishedFundingReleaseManagementMigrator, PublishedFundingReleaseManagementMigrator>();
            builder.AddSingleton<IReleaseManagementRepository, ReleaseManagementRepository>((svc) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                Configuration.Bind("releaseManagementSql", sqlSettings);
                SqlConnectionFactory factory = new SqlConnectionFactory(sqlSettings);

                SqlPolicyFactory sqlPolicyFactory = new SqlPolicyFactory();

                ExternalApiQueryBuilder externalApiQueryBuilder = new ExternalApiQueryBuilder();
                return new ReleaseManagementRepository(factory, sqlPolicyFactory, externalApiQueryBuilder);
            });

            builder.AddReleaseManagementServices(Configuration);
        }
    }
}