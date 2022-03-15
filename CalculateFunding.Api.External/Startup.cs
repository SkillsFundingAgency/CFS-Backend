using AutoMapper;
using CalculateFunding.Api.External.Middleware;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.MappingProfiles;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Api.External.V4.IoC;
using CalculateFunding.Common.Config.ApiClient.Calcs;
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
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Migration;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.IoC;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Polly.Bulkhead;
using Serilog;
using System.Linq;
using BlobClient = CalculateFunding.Common.Storage.BlobClient;
using IBlobClient = CalculateFunding.Common.Storage.IBlobClient;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using LocalBlobClient = CalculateFunding.Services.Core.AzureStorage.BlobClient;
using LocalIBlobClient = CalculateFunding.Services.Core.Interfaces.AzureStorage.IBlobClient;
using SwaggerSetup = CalculateFunding.Api.External.Swagger.SwaggerSetup;

namespace CalculateFunding.Api.External
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
            services.AddSingleton(Configuration);
            services.AddCaching(Configuration);

            IConfigurationSection azureADConfig = Configuration.GetSection("AzureAD");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{azureADConfig.GetValue<string>("Authority")}/{azureADConfig.GetValue<string>("TenantId")}/";
                    options.Audience = azureADConfig.GetValue<string>("Audience");
                });

            services.AddVersionedApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                })
                .AddMvcOptions(options =>
                {
                    options.OutputFormatters.RemoveType<StringOutputFormatter>();

                    NewtonsoftJsonOutputFormatter jFormatter =
                        options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(NewtonsoftJsonOutputFormatter)) as
                            NewtonsoftJsonOutputFormatter;
                    jFormatter?.SupportedMediaTypes.Clear();
                    jFormatter?.SupportedMediaTypes.Add("application/atom+json");
                    jFormatter?.SupportedMediaTypes.Add("application/json");
                    jFormatter?.SupportedMediaTypes.Add("application/problem+json");
                    jFormatter?.SupportedMediaTypes.Add("application/problem+xml");
                });

            // If using Kestrel:
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            // If using IIS:
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddApiVersioning(
                o =>
                {
                    o.ReportApiVersions = true;
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.DefaultApiVersion = ApiVersion.Default;
                });

            RegisterComponents(services);
            SwaggerSetup.ConfigureSwaggerServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime applicationLifetime, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            // Always use swagger for the external API, as downstreams read the documentation
            app.ConfigureSwagger(provider: provider);

            app.MapWhen(
                    context => !context.Request.Path.Value.StartsWith("/docs"),
                    appBuilder =>
                    {
                        appBuilder.UseMiddleware<LoggedInUserMiddleware>();
                        appBuilder.UseRouting();
                        appBuilder.UseAuthentication();
                        appBuilder.UseAuthorization();
                        appBuilder.UseAuthenticatedHealthCheckMiddleware();
                        appBuilder.UseMiddleware<ContentTypeCheckMiddleware>();
                        appBuilder.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });

            applicationLifetime.ApplicationStarted.Register(() => OnStartUp(app));
        }

        private void OnStartUp(IApplicationBuilder app)
        {
            IFeedItemPreloader feedItemPreLoader = app.ApplicationServices.GetService<IFeedItemPreloader>();

            feedItemPreLoader.EnsureFoldersExists();
            feedItemPreLoader.BeginFeedItemPreLoading();
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder.AddAppConfiguration();

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton<IUniqueIdentifierProvider, UniqueIdentifierProvider>();

            builder
                .AddSingleton<IPublishedFundingQueryBuilder, PublishedFundingQueryBuilder>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddFeatureToggling(Configuration);

            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();
            builder.AddSingleton<ISpecificationIdServiceRequestValidator, PublishSpecificationValidator>();

            // Register v3 services
            builder
                .AddSingleton<IFundingFeedService, FundingFeedService>();

            builder
               .AddSingleton<IFundingFeedItemByIdService, FundingFeedItemByIdService>();

            builder
                .AddSingleton<IFileSystemCache, FileSystemCache>()
                .AddSingleton<IFileSystemAccess, FileSystemAccess>()
                .AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();

            builder.AddSingleton<IFeedItemPreloader, FeedItemPreLoader>()
                .AddSingleton<IFeedItemPreloaderSettings>(ctx =>
                {
                    FeedItemPreLoaderSettings settings = new FeedItemPreLoaderSettings();

                    Configuration.Bind("feeditempreloadersettings", settings);

                    return settings;
                });

            builder.AddSingleton<IExternalApiFileSystemCacheSettings>(ctx =>
            {
                ExternalApiFileSystemCacheSettings settings = new ExternalApiFileSystemCacheSettings();

                Configuration.Bind("externalapifilesystemcachesettings", settings);

                return settings;
            });

            builder.AddSingleton<IExternalEngineOptions>(ctx =>
            {
                ExternalEngineOptions settings = new ExternalEngineOptions();

                Configuration.Bind("externalengineoptions", settings);

                return settings;
            });

            builder.AddSingleton<IPublishedFundingRetrievalService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "publishedfunding";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                IExternalApiResiliencePolicies resiliencePolicies = ctx.GetService<IExternalApiResiliencePolicies>();
                ILogger logger = ctx.GetService<ILogger>();
                IFileSystemCache fileSystemCache = ctx.GetService<IFileSystemCache>();
                IExternalApiFileSystemCacheSettings settings = ctx.GetService<IExternalApiFileSystemCacheSettings>();

                return new PublishedFundingRetrievalService(blobClient, resiliencePolicies, fileSystemCache, logger, settings);
            });

            builder.AddSingleton<IPublishedFundingRepository, PublishedFundingRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.ContainerName = "publishedfunding";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);
                IPublishedFundingQueryBuilder publishedFundingQueryBuilder = ctx.GetService<IPublishedFundingQueryBuilder>();

                return new PublishedFundingRepository(calcsCosmosRepostory, publishedFundingQueryBuilder);
            });

            builder.AddScoped<IReleaseManagementDataTableImporter, ReleaseManagementDataTableImporter>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                Configuration.Bind("releaseManagementSql", sqlSettings);

                SqlConnectionFactory sqlConnectionFactory = new SqlConnectionFactory(sqlSettings);

                return new ReleaseManagementDataTableImporter(sqlConnectionFactory);
            });

            // Register dependencies
            builder
                .AddSingleton<IFundingFeedSearchService, FundingFeedSearchService>()
                .AddSingleton<IHealthChecker, FundingFeedSearchService>();

            builder
                .AddSingleton<IFundingStreamService, FundingStreamService>();
            builder
                .AddSingleton<IPublishedProviderRetrievalService, PublishedProviderRetrievalService>();

            builder.AddSearch(Configuration);
            builder
               .AddSingleton<ISearchRepository<PublishedFundingIndex>, SearchRepository<PublishedFundingIndex>>();

            builder.AddSingleton<IProfilingService, ProfilingService>()
                .AddSingleton<IHealthChecker, ProfilingService>();

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.External");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.External");
            builder.AddLogging("CalculateFunding.Api.External");
            builder.AddTelemetry();


            builder.AddHttpContextAccessor();
            builder.AddPolicySettings(Configuration);

            builder.AddSingleton<IPublishingResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies()
                {
                    FundingFeedSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    PublishedFundingBlobRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedFundingRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ReleaseManagementRepository = Polly.Policy.NoOpAsync(), // TODO: Add SQL policies
                    ProfilingApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IExternalApiResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ExternalApiResiliencePolicies()
                {
                    PublishedProviderBlobRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedFundingBlobRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedFundingRepositoryPolicy = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClientPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });

            MapperConfiguration externalConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<ExternalServiceMappingProfile>();
                c.AddProfile<V4.MappingProfiles.ExternalServiceMappingProfile>();
            });

            builder.AddSingleton(externalConfig.CreateMapper());

            builder.AddAuthenticatedHealthCheckMiddleware();
            builder.AddPoliciesInterServiceClient(Configuration);
            builder.AddProfilingInterServiceClient(Configuration);
            builder.AddProvidersInterServiceClient(Configuration);
            builder.AddFundingDataServiceInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddCalculationsInterServiceClient(Configuration);

            builder.AddServiceBus(Configuration, "external");

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };

            });

            builder.AddSingleton<IProviderFundingVersionService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "publishedproviderversions";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                IExternalApiResiliencePolicies publishingResiliencePolicies = ctx.GetService<IExternalApiResiliencePolicies>();
                ILogger logger = ctx.GetService<ILogger>();
                IFileSystemCache fileSystemCache = ctx.GetService<IFileSystemCache>();
                IExternalApiFileSystemCacheSettings settings = ctx.GetService<IExternalApiFileSystemCacheSettings>();
                IPublishedFundingRepository publishedFundingRepository = ctx.GetService<IPublishedFundingRepository>();

                return new ProviderFundingVersionService(blobClient, publishedFundingRepository, logger, publishingResiliencePolicies, fileSystemCache, settings);
            });

            builder.AddSingleton<IPublishedProviderContentsGeneratorResolver>(ctx =>
            {
                PublishedProviderContentsGeneratorResolver resolver = new PublishedProviderContentsGeneratorResolver();

                resolver.Register("1.0", new Generators.Schema10.PublishedProviderContentsGenerator());
                resolver.Register("1.1", new Generators.Schema11.PublishedProviderContentsGenerator());
                resolver.Register("1.2", new Generators.Schema12.PublishedProviderContentsGenerator());

                return resolver;
            });

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
            builder.AddSingleton<IPublishedProviderContentPersistenceService, PublishedProviderContentPersistenceService>();
            builder.AddSingleton<IPublishedFundingContentsPersistenceService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "publishedfunding";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);

                IBlobClient blobClient = new BlobClient(blobContainerRepository);

                IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver = ctx.GetService<IPublishedFundingContentsGeneratorResolver>();

                ISearchRepository<PublishedFundingIndex> searchRepository = ctx.GetService<ISearchRepository<PublishedFundingIndex>>();

                IPublishingResiliencePolicies publishingResiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();

                return new PublishedFundingContentsPersistenceService(publishedFundingContentsGeneratorResolver,
                    blobClient,
                    publishingResiliencePolicies,
                    ctx.GetService<IPublishingEngineOptions>());
            });

            builder.AddSingleton<IVersionRepository<PublishedProviderVersion>, VersionRepository<PublishedProviderVersion>>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                CosmosRepository cosmos = new CosmosRepository(settings);

                return new VersionRepository<PublishedProviderVersion>(cosmos, new NewVersionBuilderFactory<PublishedProviderVersion>());
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
                .AddSingleton<LocalIBlobClient, LocalBlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "publishedfunding";

                    return new LocalBlobClient(storageSettings);
                });

            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<IJobTracker, JobTracker>();
            builder
                .AddSingleton<IPublishedProviderStatusUpdateSettings>(_ =>
                {
                    PublishedProviderStatusUpdateSettings settings = new PublishedProviderStatusUpdateSettings();

                    Configuration.Bind("PublishedProviderStatusUpdateSettings", settings);

                    return settings;
                }
                );
            builder.AddExternalApiV4Services(Configuration);
            builder.AddReleaseManagementServices(Configuration);
        }
    }
}