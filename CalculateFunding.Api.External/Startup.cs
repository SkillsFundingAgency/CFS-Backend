using System;
using System.Linq;
using AutoMapper;
using CalculateFunding.Api.External.Middleware;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.MappingProfiles;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
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
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Polly.Bulkhead;
using Serilog;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
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
                });     

            services.AddApiVersioning(
                o =>
                {
                    o.ReportApiVersions = true;
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                });

            RegisterComponents(services);

            if (Configuration.IsSwaggerEnabled())
            {
                SwaggerSetup.ConfigureSwaggerServices(services);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime applicationLifetime, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            if (Configuration.IsSwaggerEnabled())
            {
                app.ConfigureSwagger(provider: provider);
            }

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
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IPublishedFundingQueryBuilder, PublishedFundingQueryBuilder>();
            
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();
            
            builder.AddFeatureToggling(Configuration);

            // Register v3 services
            builder
                .AddSingleton<IFundingFeedService, FundingFeedService>();

            builder
               .AddSingleton<IFundingFeedItemByIdService, FundingFeedItemByIdService>();

            builder
                .AddSingleton<IFileSystemCache, FileSystemCache>()
                .AddSingleton<IFileSystemAccess, FileSystemAccess>()
                .AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();

            builder
                .AddSingleton<IBlobContainerRepository, BlobContainerRepository>();

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

            builder.AddSingleton<IPublishedFundingRetrievalService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "publishedfunding";

                IBlobClient blobClient = new BlobClient(storageSettings, ctx.GetService<IBlobContainerRepository>());

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
                    PublishedFundingRepositoryPolicy  = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClientPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });

            MapperConfiguration externalConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<ExternalServiceMappingProfile>();
            });

            builder.AddSingleton(externalConfig.CreateMapper());

            builder.AddAuthenticatedHealthCheckMiddleware();
            builder.AddTransient<ContentTypeCheckMiddleware>();
            builder.AddPoliciesInterServiceClient(Configuration);
            builder.AddProvidersInterServiceClient(Configuration);

            builder.AddSingleton<IProviderFundingVersionService>((ctx) =>
            {
                BlobStorageOptions storageSettings = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", storageSettings);

                storageSettings.ContainerName = "publishedproviderversions";

                IBlobClient blobClient = new BlobClient(storageSettings, ctx.GetService<IBlobContainerRepository>());

                IExternalApiResiliencePolicies publishingResiliencePolicies = ctx.GetService<IExternalApiResiliencePolicies>();
                ILogger logger = ctx.GetService<ILogger>();
                IFileSystemCache fileSystemCache = ctx.GetService<IFileSystemCache>();
                IExternalApiFileSystemCacheSettings settings = ctx.GetService<IExternalApiFileSystemCacheSettings>();
                IPublishedFundingRepository publishedFundingRepository = ctx.GetService<IPublishedFundingRepository>();

                return new ProviderFundingVersionService(blobClient, publishedFundingRepository, logger, publishingResiliencePolicies, fileSystemCache, settings);
            });
            
        }
    }
}